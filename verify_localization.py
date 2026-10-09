# -*- coding: utf-8 -*-
"""
RealChute 本地化校验工具 / RealChute localization verifier

校验内容 / Checks:
  1. GameData/RealChute/Localization/{en-us,zh-cn}.cfg 的结构与编码
  2. en-us 与 zh-cn 的键集合是否完全一致（缺失/多余都报错）
  3. C# / cfg 源码中引用的键是否都在 en-us 中定义
  4. en-us 中定义的键是否都被源码引用（孤儿键警告）
  5. 每个值中的 <<n>> 占位符序号是否连续（从 1 开始，无跳号）
  6. 值里不应出现真实的换行、也不应以 # 开头
  7. en-us / zh-cn 都不带 BOM

用法 / Usage:
    python verify_localization.py
"""

import os
import re
import sys
from collections import OrderedDict

HERE = os.path.dirname(os.path.abspath(__file__))
LOCALIZATION_DIR = os.path.join(HERE, "Output", "GameData", "RealChute", "Localization")
SOURCE_DIR = os.path.join(HERE, "RealChute")
DIST_DIR = os.path.join(os.path.dirname(HERE), "RealChute")  # official distribution

KEY_RE = re.compile(r"^\s*(#RealChute_[A-Za-z0-9_]+)\s*=\s*(.*?)\s*$")
SECT_RE = re.compile(r"^\s*([A-Za-z]{2}(?:-[A-Za-z]{2})?)\s*$")
PLACEHOLDER_RE = re.compile(r"<<(\d+)>>")

errors = []
warnings = []


def report(level, msg):
    (errors if level == "E" else warnings).append(msg)


# ---------------------------------------------------------------------------
# 1. 读文件并检查编码 / Read files and check encoding
# ---------------------------------------------------------------------------
def read_cfg(path):
    with open(path, "rb") as fh:
        raw = fh.read()
    if raw.startswith(b"\xef\xbb\xbf"):
        report("E", f"{os.path.basename(path)}: 带 UTF-8 BOM，应为无 BOM")
    try:
        return raw.decode("utf-8")
    except UnicodeDecodeError as exc:
        report("E", f"{os.path.basename(path)}: 不是合法 UTF-8 ({exc})")
        return None


def parse_localization(text, lang_wanted, path):
    """解析 Localization{ <lang>{ ... } } ，返回 {key: value}"""
    name = os.path.basename(path)
    inside_localization = False
    inside_lang = False
    depth = 0
    result = OrderedDict()
    lang_seen = set()

    for lineno, line in enumerate(text.splitlines(), 1):
        stripped = line.strip()
        if not stripped or stripped.startswith("//"):
            continue

        if stripped == "Localization":
            inside_localization = True
            depth = 0
            continue
        if not inside_localization:
            continue

        if stripped == "{":
            depth += 1
            continue
        if stripped == "}":
            depth -= 1
            if depth <= 1:
                inside_lang = False
            if depth <= 0:
                inside_localization = False
            continue

        m = SECT_RE.match(stripped)
        if m and depth == 1:
            inside_lang = (m.group(1) == lang_wanted)
            lang_seen.add(m.group(1))
            continue

        m = KEY_RE.match(line)
        if m and inside_lang and depth >= 2:
            key, value = m.group(1), m.group(2)
            if key in result:
                report("E", f"{name}:{lineno}: 键重复 {key}")
            result[key] = value
            continue

        # 在语言块里出现既不是键也不是大括号的行
        if inside_lang and depth >= 2:
            report("E", f"{name}:{lineno}: 无法解析的行: {stripped[:80]}")

    if not lang_seen:
        report("E", f"{name}: 没有找到任何语言段")
    if lang_wanted not in lang_seen:
        report("E", f"{name}: 缺少语言段 '{lang_wanted}'（找到 {sorted(lang_seen)}）")
    return result


# ---------------------------------------------------------------------------
# 2. 收集源码里引用的键 / Collect keys referenced by the sources
# ---------------------------------------------------------------------------
def collect_cs_keys():
    """Localization.Get("Tag") -> #RealChute_Tag ; "#RealChute_x" 字面量 -> #RealChute_x"""
    keys = {}
    # 直接的 Localization.Get("Tag")
    get_re = re.compile(r'Localization\.Get\(\s*"([A-Za-z0-9_]+)"')
    # 嵌套三元里的 Localization.Get(cond ? "A" : "B")，两个键都算被引用
    nested_re = re.compile(r'Localization\.Get\([^()]*\?\s*"([A-Za-z0-9_]+)"\s*:\s*"([A-Za-z0-9_]+)"')
    tag_re = re.compile(r'"(#RealChute_[A-Za-z0-9_]+)"')
    for dirpath, _dirs, files in os.walk(SOURCE_DIR):
        for fn in files:
            if not fn.endswith(".cs"):
                continue
            full = os.path.join(dirpath, fn)
            rel = os.path.relpath(full, HERE)
            with open(full, "rb") as fh:
                data = fh.read().decode("utf-8-sig")
            for i, line in enumerate(data.splitlines(), 1):
                for m in get_re.finditer(line):
                    keys.setdefault("#RealChute_" + m.group(1), []).append(f"{rel}:{i}")
                for m in nested_re.finditer(line):
                    for g in (1, 2):
                        keys.setdefault("#RealChute_" + m.group(g), []).append(f"{rel}:{i}")
                for m in tag_re.finditer(line):
                    keys.setdefault(m.group(1), []).append(f"{rel}:{i}")
    return keys


def collect_cfg_keys():
    """cfg 里的 #RealChute_xxx 引用（title/description/manufacturer 等）"""
    keys = {}
    tag_re = re.compile(r'#RealChute_[A-Za-z0-9_]+')
    for base in (DIST_DIR,):
        if not os.path.isdir(base):
            continue
        for dirpath, _dirs, files in os.walk(base):
            for fn in files:
                if not fn.endswith(".cfg"):
                    continue
                full = os.path.join(dirpath, fn)
                # 跳过本地化字典本身
                if os.path.basename(os.path.dirname(full)) == "Localization":
                    continue
                rel = os.path.relpath(full, HERE)
                with open(full, "rb") as fh:
                    data = fh.read().decode("utf-8-sig", errors="replace")
                for i, line in enumerate(data.splitlines(), 1):
                    if line.strip().startswith("//"):
                        continue
                    for m in tag_re.finditer(line):
                        keys.setdefault(m.group(0), []).append(f"{rel}:{i}")
    return keys


# ---------------------------------------------------------------------------
# 主流程 / Main
# ---------------------------------------------------------------------------
def main():
    en_path = os.path.join(LOCALIZATION_DIR, "en-us.cfg")
    zh_path = os.path.join(LOCALIZATION_DIR, "zh-cn.cfg")

    for p in (en_path, zh_path):
        if not os.path.isfile(p):
            report("E", f"缺少文件: {p}")
    if errors:
        return finish()

    en = parse_localization(read_cfg(en_path), "en-us", en_path)
    zh = parse_localization(read_cfg(zh_path), "zh-cn", zh_path)

    print(f"en-us 键数: {len(en)}")
    print(f"zh-cn 键数: {len(zh)}")

    # 键集合一致性
    only_en = sorted(set(en) - set(zh))
    only_zh = sorted(set(zh) - set(en))
    for k in only_en:
        report("E", f"zh-cn 缺少键: {k}")
    for k in only_zh:
        report("E", f"zh-cn 多出键（en-us 未定义）: {k}")

    # 值检查
    for label, table in (("en-us", en), ("zh-cn", zh)):
        for key, value in table.items():
            if value.startswith("#"):
                report("E", f"{label}: {key} 的值以 # 开头")
            if "\n" in value or "\r" in value:
                report("E", f"{label}: {key} 的值含真实换行（应写 \\n）")
            nums = sorted(int(n) for n in PLACEHOLDER_RE.findall(value))
            if nums:
                expected = list(range(1, len(nums) + 1))
                if nums != expected:
                    report("E", f"{label}: {key} 占位符序号异常 {nums}，应为 1..{len(nums)}")

    # 占位符在两个语言之间必须一致
    for key in set(en) & set(zh):
        a = sorted(int(n) for n in PLACEHOLDER_RE.findall(en[key]))
        b = sorted(int(n) for n in PLACEHOLDER_RE.findall(zh[key]))
        if a != b:
            report("E", f"{key}: 占位符数量不一致 en={a} zh={b}")

    # 源码引用覆盖
    cs_keys = collect_cs_keys()
    cfg_keys = collect_cfg_keys()
    referenced = {}
    for src, table in (("C#", cs_keys), ("cfg", cfg_keys)):
        for k, locs in table.items():
            referenced.setdefault(k, []).extend(f"[{src}] {l}" for l in locs)

    print(f"源码引用的键数: {len(referenced)} (C# {len(cs_keys)} + cfg {len(cfg_keys)})")

    for k in sorted(set(referenced) - set(en)):
        report("E", f"源码引用了但 en-us 未定义的键: {k}  <- {referenced[k][0]}")

    orphan = sorted(set(en) - set(referenced))
    for k in orphan:
        report("W", f"en-us 定义了但源码未引用的键: {k}")

    return finish()


def finish():
    print()
    for w in warnings:
        print(f"[警告 WARN] {w}")
    for e in errors:
        print(f"[错误 ERROR] {e}")
    print()
    if errors:
        print(f"校验失败 / FAILED: {len(errors)} 个错误, {len(warnings)} 个警告")
        return 1
    print(f"校验通过 / PASSED ({len(warnings)} 个警告)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
