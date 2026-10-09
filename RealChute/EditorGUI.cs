using System;
using System.Collections.Generic;
using System.Text;
using ClickThroughFix;
using RealChute.Extensions;
using RealChute.Libraries;
using RealChute.Libraries.Presets;
using UnityEngine;

/* RealChute was made by Christophe Savard (stupid_chris). You are free to copy, fork, and modify RealChute as you see
 * fit. However, redistribution is only permitted for unmodified versions of RealChute, and under attribution clause.
 * If you want to distribute a modified version of RealChute, be it code, textures, configs, or any other asset and
 * piece of work, you must get my explicit permission on the matter through a private channel, and must also distribute
 * it through the attribution clause, and must make it clear to anyone using your modification of my work that they
 * must report any problem related to this usage to you, and not to me. This clause expires if I happen to be
 * inactive (no connection) for a period of 90 days on the official KSP forums. In that case, the license reverts
 * back to CC-BY-NC-SA 4.0 INTL.*/

namespace RealChute
{
    public class EditorGUI
    {
        #region Propreties
        private Part Part => this.pChute.part;

        private RealChuteModule RCModule => this.pChute.rcModule;

        private List<ChuteTemplate> Chutes => this.pChute.chutes;

        private List<SizeNode> Sizes => this.pChute.sizes;
        #endregion

        #region Fields
        private readonly ProceduralChute pChute;
        internal Rect windowDrag, closeButtonRect, failedWindow, successfulWindow;
        internal Rect presetsWindow, presetsSaveWindow, presetsWarningWindow;
        private readonly int mainId = Guid.NewGuid().GetHashCode(), failedId = Guid.NewGuid().GetHashCode(), successId = Guid.NewGuid().GetHashCode();
        private readonly int presetSaveId = Guid.NewGuid().GetHashCode(), presetWarningId = Guid.NewGuid().GetHashCode();
        internal int matX = (int)(500 * GameSettings.UI_SCALE), matY = (int)(370 * GameSettings.UI_SCALE);
        private Vector2 mainScroll, failedScroll;
        private Vector2 presetScroll;
        internal string presetName = string.Empty, presetDescription = string.Empty;
        internal bool warning = false;
        internal bool visible = false, failedVisible, successfulVisible;
        internal bool presetVisible, presetSaveVisible, presetWarningVisible;
        private bool saveWarning;
        internal string[] cases = new string[0], canopies = new string[0], models = new string[0];

        internal static Rect mainWindow;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new blank EditorGUI object
        /// </summary>
        public EditorGUI() { }

        /// <summary>
        /// Creates a new RCEditorGUI
        /// </summary>
        /// <param name="pChute">ProceduralChute module to create the object from</param>
        public EditorGUI(ProceduralChute pChute)
        {
            this.pChute = pChute;
        }
        #endregion

        #region Methods
        //Renders the RealChute editor GUI
        internal void RenderGUI()
        {
            if (!HighLogic.LoadedSceneIsEditor) { return; }

            GUI.skin = HighLogic.Skin;
            if (this.visible)
            {
                mainWindow = ClickThruBlocker.GUILayoutWindow(this.mainId, mainWindow, Window, Localization.Get("Editor_Window_Title", RCUtils.AssemblyVersion), GUIUtils.ScaledWindow, GUILayout.MaxWidth(420f * GameSettings.UI_SCALE), GUILayout.MaxHeight((Screen.height - 375f) * GameSettings.UI_SCALE));
            }
            foreach (ChuteTemplate chute in this.Chutes)
            {
                TemplateGUI gui = chute.templateGUI;
                if (gui.materialsVisible)
                {
                    gui.materialsWindow = ClickThruBlocker.GUILayoutWindow(gui.matId, gui.materialsWindow, gui.MaterialsWindow, Localization.Get("Editor_Materials_Title"), GUIUtils.ScaledWindow, GUILayout.MaxWidth(375f * GameSettings.UI_SCALE), GUILayout.MaxHeight(275f * GameSettings.UI_SCALE));
                }
            }
            if (this.failedVisible)
            {
                this.failedWindow = ClickThruBlocker.GUILayoutWindow(this.failedId, this.failedWindow, ApplicationFailed, Localization.Get("Editor_Error_Title"), GUIUtils.ScaledWindow, GUILayout.MaxWidth(300f * GameSettings.UI_SCALE), GUILayout.MaxHeight(300f * GameSettings.UI_SCALE));
            }
            if (this.successfulVisible)
            {
                this.successfulWindow = ClickThruBlocker.GUILayoutWindow(this.successId, this.successfulWindow, ApplicationSucceeded, Localization.Get("Editor_Success_Title"), GUIUtils.ScaledWindow, GUILayout.MaxWidth(300f * GameSettings.UI_SCALE), GUILayout.MaxHeight(200f * GameSettings.UI_SCALE), GUILayout.ExpandHeight(true));
            }
            if (this.presetVisible)
            {
                this.presetsWindow = ClickThruBlocker.GUILayoutWindow(this.pChute.presetId, this.presetsWindow, Presets, Localization.Get("Editor_Presets_Title"), GUIUtils.ScaledWindow, GUILayout.MaxWidth(400f * GameSettings.UI_SCALE), GUILayout.MaxHeight(500f * GameSettings.UI_SCALE));
            }
            if (this.presetSaveVisible)
            {
                this.presetsSaveWindow = ClickThruBlocker.GUILayoutWindow(this.presetSaveId, this.presetsSaveWindow, SavePreset, Localization.Get("Editor_SavePreset_Title"), GUIUtils.ScaledWindow, GUILayout.MaxWidth(350f * GameSettings.UI_SCALE), GUILayout.MaxHeight(400f * GameSettings.UI_SCALE));
            }
            if (this.presetWarningVisible)
            {
                this.presetsWarningWindow = ClickThruBlocker.GUILayoutWindow(this.presetWarningId, this.presetsWarningWindow, PresetWarning, Localization.Get("Editor_Warning_Title"), GUIUtils.ScaledWindow, GUILayout.Width(200f * GameSettings.UI_SCALE), GUILayout.Height(100f * GameSettings.UI_SCALE));
            }
        }

        //Main GUI window
        private void Window(int id)
        {
            //Top right close button
            Color temp = GUI.color;
            GUI.color = new Color(1f, 0.3f, 0.3f, 1f);
            bool shouldClose = GUI.Button(this.closeButtonRect, "X", GUIUtils.ScaledButton);
            GUI.color = temp;

            GUI.DragWindow(this.windowDrag);

            GUILayout.BeginVertical();

            #region Info labels
            //Header labels
            StringBuilder builder = StringBuilderCache.Acquire();
            builder.Append(Localization.Get("Editor_SelectedPart")).AppendLine(this.Part.partInfo.title);
            builder.Append(Localization.Get("Common_SymmetryCounterparts")).AppendLine(this.Part.symmetryCounterparts.Count.ToString());
            builder.Append(Localization.Get("Common_CaseMass")).Append(this.RCModule.caseMass.ToString("0.000")).Append("t");
            if (this.Sizes.Count > 0) { builder.Append("\t\t").Append(Localization.Get("Common_CaseCost")).Append(this.Sizes[this.pChute.size].Cost.ToString("0.#")).Append("F"); }
            builder.Append("\n").Append(Localization.Get("Common_TotalPartMass")).Append(this.Part.TotalMass().ToString("0.000")).Append("t");
            builder.Append("\t").Append(Localization.Get("Common_TotalPartCost")).Append(this.Part.TotalCost().ToString("0.#")).Append("F");
            GUILayout.Label(builder.ToStringAndRelease(), GUIUtils.ScaledLabel);
            #endregion

            #region Presets
            //Presets buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Localization.Get("Editor_SelectPreset"), GUIUtils.ScaledButton)) { this.presetVisible = !this.presetVisible; }

            if (GUILayout.Button(Localization.Get("Editor_SaveAsPreset"), GUIUtils.ScaledButton)) { this.presetSaveVisible = !this.presetSaveVisible; }
            GUILayout.EndHorizontal();
            #endregion

            //Scroll being
            this.mainScroll = GUILayout.BeginScrollView(this.mainScroll);

            #region Planet selector
            //Target planet selection
            GUILayout.Space(10f * GameSettings.UI_SCALE);
            GUILayout.BeginHorizontal(GUILayout.Height(30f * GameSettings.UI_SCALE));
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Label(Localization.Get("Editor_TargetPlanet"), GUIUtils.ScaledLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            this.pChute.planets = GUILayout.SelectionGrid(this.pChute.planets, AtmoPlanets.Instance.BodyNames, 4, GUIUtils.ScaledButton, GUILayout.Width(250f * GameSettings.UI_SCALE));
            GUILayout.EndHorizontal();
            this.pChute.body = AtmoPlanets.Instance.GetBody(this.pChute.planets);
            #endregion

            #region Size cyclers
            //Size selection
            if (this.Sizes.Count > 0)
            {
                GUILayout.BeginHorizontal(GUILayout.Height(20f * GameSettings.UI_SCALE));
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                GUILayout.Label(Localization.Get("Editor_CyclePartSize"), GUIUtils.ScaledLabel);
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(Localization.Get("Editor_PreviousSize"), GUIUtils.ScaledButton, GUILayout.Width(125f * GameSettings.UI_SCALE)))
                {
                    this.pChute.size--;
                    if (this.pChute.size < 0) { this.pChute.size = this.Sizes.Count - 1; }
                }
                if (GUILayout.Button(Localization.Get("Editor_NextSize"), GUIUtils.ScaledButton, GUILayout.Width(125f * GameSettings.UI_SCALE)))
                {
                    this.pChute.size++;
                    if (this.pChute.size > this.Sizes.Count - 1) { this.pChute.size = 0; }
                }
                GUILayout.EndHorizontal();
            }
            #endregion

            GUILayout.Space(5f * GameSettings.UI_SCALE);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();

            //Main chute texture selector
            GUILayout.Space(5f * GameSettings.UI_SCALE);
            this.Chutes[0].templateGUI.TextureSelector();

            #region General
            //Materials editor
            GUILayout.Space(5f * GameSettings.UI_SCALE);
            this.Chutes[0].templateGUI.MaterialsSelector();

            //MustGoDown
            GUIUtils.CreateTwinToggle(Localization.Get("Editor_MustGoDown"), ref this.pChute.mustGoDown, mainWindow.width);

            //DeployOnGround
            GUIUtils.CreateTwinToggle(Localization.Get("Editor_DeployOnGround"), ref this.pChute.deployOnGround, mainWindow.width);

            //Timer
            GUIUtils.CreateTimeEntryArea(Localization.Get("Editor_DeploymentTimer"), ref this.pChute.timer, 0f, 3600f);

            //Spares
            GUIUtils.CreateEmptyEntryArea(Localization.Get("Editor_SpareChutes"), ref this.pChute.spares, -1f, 10f);

            //CutSpeed
            GUIUtils.CreateEntryArea(Localization.Get("Editor_AutocutSpeed"), ref this.pChute.cutSpeed, 0.01f, 100f);

            //LandingAlt
            GUIUtils.CreateEntryArea(Localization.Get("Editor_LandingAlt"), ref this.pChute.landingAlt, 0f, (float)this.pChute.body.GetMaxAtmosphereAltitude());
            #endregion

            #region Main
            //Indicator label
            GUILayout.Space(10f * GameSettings.UI_SCALE);
            GUILayout.Label("________________________________________________", GUIUtils.BoldLabel);
            GUILayout.Label(Localization.Get("Editor_MainChute"), GUIUtils.BoldLabel, GUILayout.Width(150f * GameSettings.UI_SCALE));
            GUILayout.Label("‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾", GUIUtils.BoldLabel);

            this.Chutes[0].templateGUI.Calculations();
            #endregion

            #region Secondary
            if (this.pChute.secondaryChute)
            {
                for (int i = 1; i < this.Chutes.Count; i++)
                {
                    ChuteTemplate chute = this.Chutes[i];

                    //Indicator label
                    GUILayout.Space(10f * GameSettings.UI_SCALE);
                    GUILayout.Label("________________________________________________", GUIUtils.BoldLabel);
                    GUILayout.Label(RCUtils.ParachuteNumber(i) + ":", GUIUtils.BoldLabel, GUILayout.Width(150f * GameSettings.UI_SCALE));
                    GUILayout.Label("‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾", GUIUtils.BoldLabel);

                    //Texture selector
                    GUILayout.Space(5f * GameSettings.UI_SCALE);
                    chute.templateGUI.TextureSelector();

                    //Materials editor
                    GUILayout.Space(5f * GameSettings.UI_SCALE);
                    chute.templateGUI.MaterialsSelector();

                    chute.templateGUI.Calculations();
                }
            }
            #endregion

            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(5f * GameSettings.UI_SCALE);
            GUILayout.EndScrollView();
            //Scroll end

            #region Application
            GUILayout.Space(5f * GameSettings.UI_SCALE);
            if (GUILayout.Button(Localization.Get("Editor_ApplySettings"), GUIUtils.ScaledButton))
            {
                this.pChute.Apply(false);
            }

            if (this.Part.symmetryCounterparts.Count > 0)
            {
                if (GUILayout.Button(Localization.Get("Editor_ApplyToSymmetry"), GUIUtils.ScaledButton))
                {
                    this.pChute.Apply(true);
                }
            }
            #endregion

            GUILayout.EndVertical();

            if (shouldClose)
            {
                this.pChute.HideEditor();
            }
        }

        //Failure notice
        private void ApplicationFailed(int id)
        {
            GUILayout.Label(Localization.Get("Editor_InvalidParameters"), GUIUtils.ScaledLabel);
            GUILayout.Space(10);
            this.failedScroll = GUILayout.BeginScrollView(this.failedScroll, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.MaxHeight(200f * GameSettings.UI_SCALE));
            this.pChute.CreateErrors();
            GUILayout.EndScrollView();
            if (GUILayout.Button(Localization.Get("Common_Close"), GUIUtils.ScaledButton))
            {
                this.failedVisible = false;
            }
        }

        //Success notice
        private void ApplicationSucceeded(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(Localization.Get("Editor_ApplicationSucceeded"), GUIUtils.ScaledLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (this.warning)
            {
                GUILayout.Label(Localization.Get("Editor_MassTooHigh"), GUIUtils.RedLabel);
            }

            if (GUILayout.Button(Localization.Get("Common_Close"), GUIUtils.ScaledButton))
            {
                this.successfulVisible = false;
            }
        }

        //Presets selection window
        private void Presets(int id)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            this.presetScroll = GUILayout.BeginScrollView(this.presetScroll, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.Width(200f * GameSettings.UI_SCALE));
            string[] current = PresetsLibrary.Instance.GetRelevantPresets(this.Chutes.Count);
            string p = string.Empty;
            if (current.Length > 0)
            {
                this.pChute.presetId = GUILayout.SelectionGrid(this.pChute.presetId, current, 1, GUIUtils.ScaledButton);
                p = current[this.pChute.presetId];
            }
            else { GUILayout.Label(Localization.Get("Editor_NoSavedPresets"), GUIUtils.ScaledLabel); }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUILayout.Width(200f * GameSettings.UI_SCALE));
            if (!string.IsNullOrEmpty(p)) { GUILayout.Label(Localization.Get("Editor_Description", PresetsLibrary.Instance.GetPreset(p).Description), GUIUtils.ScaledLabel); }
            else { GUILayout.Label("---", GUIUtils.ScaledLabel); }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (PresetsLibrary.Instance.Presets.Count > 0)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Localization.Get("Editor_SelectPresetButton"), GUIUtils.ScaledButton))
                {
                    this.pChute.ApplyPreset();
                    this.presetVisible = false;
                }

                if (GUILayout.Button(Localization.Get("Editor_DeletePreset"), GUIUtils.ScaledButton))
                {
                    this.saveWarning = false;
                    this.presetWarningVisible = true;
                }
                GUILayout.EndHorizontal();
            }
            if (GUILayout.Button(Localization.Get("Common_Cancel"), GUIUtils.ScaledButton)) { this.presetVisible = false; }
            GUILayout.EndVertical();
        }

        //Presets saving window
        private void SavePreset(int id)
        {
            GUILayout.BeginVertical();
            GUILayout.Label(Localization.Get("Editor_PresetName"), GUIUtils.ScaledButton);
            this.presetName = GUILayout.TextField(this.presetName);
            GUILayout.Label(Localization.Get("Editor_PresetDescription"), GUIUtils.ScaledButton);
            this.presetDescription = GUILayout.TextArea(this.presetDescription, GUIUtils.ScaledTextField, GUILayout.Height(100f * GameSettings.UI_SCALE));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Localization.Get("Editor_SaveButton"), GUIUtils.ScaledButton))
            {
                if (this.presetName == string.Empty) { RCUtils.PopupDialog(Localization.Get("Editor_PresetError_Title"), Localization.Get("Editor_PresetNameEmpty"), Localization.Get("Common_Close")); }
                else if (PresetsLibrary.Instance.ContainsPreset(this.presetName))
                {
                    this.presetWarningVisible = true;
                    this.saveWarning = true;
                }
                else if (this.pChute.GetErrors(true).Count != 0 || this.pChute.GetErrors(false).Count != 0) { this.failedVisible = true; }
                else
                {
                    this.pChute.CreatePreset();
                    this.presetSaveVisible = false;
                }
            }
            if (GUILayout.Button(Localization.Get("Common_Cancel"), GUIUtils.ScaledButton)) { this.presetSaveVisible = false; }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        //Preset saving confirmation window
        private void PresetWarning(int id)
        {
            GUILayout.BeginVertical();
            GUILayout.Label(this.saveWarning ? Localization.Get("Editor_PresetOverwriteWarning") : Localization.Get("Editor_PresetDeleteWarning"), GUIUtils.RedLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Localization.Get("Common_Yes"), GUIUtils.ScaledButton))
            {
                Preset preset = this.saveWarning ? PresetsLibrary.Instance.GetPreset(this.presetName) : PresetsLibrary.Instance.GetPreset(this.pChute.presetId, this.pChute.chutes.Count);
                Debug.Log("[RealChute]: Deleting the \"" + preset.Name + "\" preset from the database.");
                PresetsLibrary.Instance.DeletePreset(preset);
                if (this.saveWarning)
                {
                   this.pChute.CreatePreset();
                   this.presetSaveVisible = false;
                }
                else { this.pChute.presetId = 0; }
                this.presetWarningVisible = false;
            }
            if (GUILayout.Button(Localization.Get("Common_No"), GUIUtils.ScaledButton)) { this.presetWarningVisible = false; }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        //Resets the main window location to its default value
        internal static void ResetWindowLocation()
        {
            mainWindow = new Rect((Screen.width / 2f) - (200f * GameSettings.UI_SCALE), (Screen.height / 2f) - (300f * GameSettings.UI_SCALE), 400f * GameSettings.UI_SCALE, 600f * GameSettings.UI_SCALE);
        }
        #endregion
    }
}