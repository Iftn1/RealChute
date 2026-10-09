# -*- coding: utf-8 -*-
"""
校验 DLL 是否真的内含全部本地化键字面量 / Verify every localization tag is inside the built DLL

用法 / Usage:
    python verify_dll_strings.py
"""

import os
import re
import sys
import pathlib

HERE = os.path.dirname(os.path.abspath(__file__))
SOURCE_DIR = os.path.join(HERE, "RealChute")
DLL = os.path.join(HERE, "Output", "GameData", "RealChute", "Plugins", "RealChute.dll")
LOCALIZATION_DIR = os.path.join(HERE, "Output", "GameData", "RealChute", "Localization")

errors = []


def collect_source_tags():
    """C# 源里的 Localization.Get("Tag") 与被 "#RealChute_x" 引用的键"""
    direct = set()
    prefixed = set()
    get_re = re.compile(r'Localization\.Get\(\s*"([A-Za-z0-9_]+)"')
    nested_re = re.compile(r'Localization\.Get\([^()]*\?\s*"([A-Za-z0-9_]+)"\s*:\s*"([A-Za-z0-9_]+)"')
    tag_re = re.compile(r'"(#RealChute_[A-Za-z0-9_]+)"')
    for dirpath, _dirs, files in os.walk(SOURCE_DIR):
        for fn in files:
            if not fn.endswith(".cs"):
                continue
            data = pathlib.Path(dirpath, fn).read_bytes().decode("utf-8-sig")
            for m in get_re.finditer(data):
                direct.add(m.group(1))
            for m in nested_re.finditer(data):
                direct.add(m.group(1))
                direct.add(m.group(2))
            for m in tag_re.finditer(data):
                prefixed.add(m.group(1))
    return direct, prefixed


def collect_cfg_tags():
    """非本地化 cfg 里引用的 #RealChute_x"""
    tags = set()
    tag_re = re.compile(r'#RealChute_[A-Za-z0-9_]+')
    for base in (os.path.join(HERE, "Output", "GameData", "RealChute"),
                 os.path.join(os.path.dirname(HERE), "RealChute")):
        if not os.path.isdir(base):
            continue
        for dirpath, _dirs, files in os.walk(base):
            if os.path.basename(dirpath) == "Localization":
                continue
            for fn in files:
                if not fn.endswith(".cfg"):
                    continue
                for line in pathlib.Path(dirpath, fn).read_bytes().decode("utf-8-sig", "replace").splitlines():
                    if line.strip().startswith("//"):
                        continue
                    tags.update(tag_re.findall(line))
    return tags


def main():
    if not os.path.isfile(DLL):
        print(f"缺少 DLL / DLL not found: {DLL}")
        return 1

    dll = pathlib.Path(DLL).read_bytes()
    # 字符串字面量以 UTF-16LE 存放在 #US 堆
    utf16 = dll.decode("utf-16-le", errors="ignore")

    direct, prefixed = collect_source_tags()
    cfg_tags = collect_cfg_tags()

    print(f"DLL: {DLL}")
    print(f"  大小 / size: {len(dll)} bytes")
    print(f"C# 直接引用 Localization.Get 的键 : {len(direct)}")
    print(f"C# 以 #RealChute_x 引用的键      : {len(prefixed)}")
    print(f"cfg 引用的键                     : {len(cfg_tags)}")

    # 1. 每个 Localization.Get("Tag") 的 "Tag" 字面量必须在 DLL 里（UTF-16LE，#US 堆）
    #    注意：同一个字符串字面量在 #US 堆里只存一份，且可能被 UTF-16 解码边界错开
    # 字符串字面量的存放位置分两种（实测）：
    #   - Localization.Get("Tag") / "guiName" 这类运行期字符串 -> #US 堆，UTF-16LE
    #   - 特性实参 [KSPAction("...")] / [KSPEvent(guiName="...")] 的命名实参 -> 元数据字符串堆，UTF-8
    # 因此两种编码都要查。
    def in_dll(s):
        return s.encode("utf-16-le") in dll or s.encode("utf-8") in dll

    missing_direct = sorted(t for t in direct if not in_dll(t))
    for t in missing_direct:
        errors.append(f'Localization.Get("{t}") 的字面量不在 DLL 中')

    # 2. KSPAction/KSPEvent/guiName 的属性字符串以 "#RealChute_x" 形式编译进 DLL，
    #    显式设置的 guiName 也写成 "#RealChute_x"，两者都带 #。
    missing_prefixed = sorted(t for t in prefixed if not in_dll(t))
    for t in missing_prefixed:
        errors.append(f"字面量 {t} 不在 DLL 中")

    # 3. cfg 引用的键应该都有定义
    en = pathlib.Path(LOCALIZATION_DIR, "en-us.cfg").read_bytes().decode("utf-8-sig")
    defined = set(re.findall(r"^\s*(#RealChute_[A-Za-z0-9_]+)\s*=", en, re.M))
    for t in sorted(cfg_tags):
        if t not in defined:
            errors.append(f"cfg 引用 {t}，但 en-us.cfg 未定义")

    # 4. 反向：en-us 里每个键都应至少被某处引用
    #    统一到“不带 # 也不带 RealChute_ 的裸标签名”这一坐标：
    #      - Localization.Get("Tag")            的实参本来就是裸标签
    #      - 属性 / guiName / cfg 里是 "#RealChute_Tag"，去掉 "#RealChute_" 后比较
    #      - 字典里定义的是 "#RealChute_Tag"，同样去掉 "#RealChute_"
    def to_bare(key):
        k = key[1:] if key.startswith("#") else key
        return k[len("RealChute_"):] if k.startswith("RealChute_") else k

    referenced = {to_bare(t) for t in direct} | {to_bare(t) for t in prefixed} | {to_bare(t) for t in cfg_tags}
    defined_tags = {to_bare(k) for k in defined}
    for t in sorted(defined_tags - referenced):
        errors.append(f"en-us.cfg 定义了 #RealChute_{t}，但没有任何源码/cfg 引用它")

    print()
    if errors:
        for e in errors:
            print(f"[错误 ERROR] {e}")
        print(f"\n校验失败 / FAILED: {len(errors)} 个错误")
        return 1

    print(f"OK: DLL 内含全部 {len(direct)} 个 Localization.Get 字面量 + {len(prefixed)} 个带 # 的字面量")
    print(f"OK: en-us.cfg 的 {len(defined)} 个键全部被引用，cfg 引用全部有定义")
    print("校验通过 / PASSED")
    return 0


if __name__ == "__main__":
    sys.exit(main())
