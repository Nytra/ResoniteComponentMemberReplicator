using System.Collections.Generic;
using ResoniteModLoader;
using FrooxEngine;
using FrooxEngine.UIX;
using Elements.Core;
using System;
using System.Reflection;
using FrooxEngine.Undo;
using HarmonyLib;
using Elements.Assets;
using FrooxEngine.ProtoFlux;
using System.IO;
using Mono.Cecil;

namespace SyncMemberManipulator
{
    public class SyncMemberManipulatorMod : ResoniteMod
    {
		public override string Name => "SyncMemberManipulator";
		public override string Author => "Nytra";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/Nytra/ResoniteSyncMemberManipulator";

		// initial
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_InitialMinWidth = new ModConfigurationKey<float>("Key_InitialMinWidth", "Key_InitialMinWidth", () => -1f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_InitialMinHeight = new ModConfigurationKey<float>("Key_InitialMinHeight", "Key_InitialMinHeight", () => 24f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_InitialPreferredWidth = new ModConfigurationKey<float>("Key_InitialPreferredWidth", "Key_InitialPreferredWidth", () => -1f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_InitialPreferredHeight = new ModConfigurationKey<float>("Key_InitialPreferredHeight", "Key_InitialPreferredHeight", () => 24f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_InitialFlexibleWidth = new ModConfigurationKey<float>("Key_InitialFlexibleWidth", "Key_InitialFlexibleWidth", () => -1f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_InitialFlexibleHeight = new ModConfigurationKey<float>("Key_InitialFlexibleHeight", "Key_InitialFlexibleHeight", () => -1f);

		[AutoRegisterConfigKey]
		static ModConfigurationKey<dummy> Key_Dummy1 = new ModConfigurationKey<dummy>("Key_Dummy1", "<size=0></size>", () => new dummy());

		// fields
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_FieldsMinWidth = new ModConfigurationKey<float>("Key_FieldsMinWidth", "Key_FieldsMinWidth", () => -1f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_FieldsMinHeight = new ModConfigurationKey<float>("Key_FieldsMinHeight", "Key_FieldsMinHeight", () => 24f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_FieldsPreferredWidth = new ModConfigurationKey<float>("Key_FieldsPreferredWidth", "Key_FieldsPreferredWidth", () => -1f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_FieldsPreferredHeight = new ModConfigurationKey<float>("Key_FieldsPreferredHeight", "Key_FieldsPreferredHeight", () => -1f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_FieldsFlexibleWidth = new ModConfigurationKey<float>("Key_FieldsFlexibleWidth", "Key_FieldsFlexibleWidth", () => 1000f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_FieldsFlexibleHeight = new ModConfigurationKey<float>("Key_FieldsFlexibleHeight", "Key_FieldsFlexibleHeight", () => -1f);

		[AutoRegisterConfigKey]
		static ModConfigurationKey<dummy> Key_Dummy2 = new ModConfigurationKey<dummy>("Key_Dummy2", "<size=0></size>", () => new dummy());

		// checkbox
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_CheckboxMinWidth = new ModConfigurationKey<float>("Key_CheckboxMinWidth", "Key_CheckboxMinWidth", () => 24f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_CheckboxMinHeight = new ModConfigurationKey<float>("Key_CheckboxMinHeight", "Key_CheckboxMinHeight", () => 24f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_CheckboxPreferredWidth = new ModConfigurationKey<float>("Key_CheckboxPreferredWidth", "Key_CheckboxPreferredWidth", () => -1f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_CheckboxPreferredHeight = new ModConfigurationKey<float>("Key_CheckboxPreferredHeight", "Key_CheckboxPreferredHeight", () => -1f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_CheckboxFlexibleWidth = new ModConfigurationKey<float>("Key_CheckboxFlexibleWidth", "Key_CheckboxFlexibleWidth", () => 1f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_CheckboxFlexibleHeight = new ModConfigurationKey<float>("Key_CheckboxFlexibleHeight", "Key_CheckboxFlexibleHeight", () => -1f);

        //[AutoRegisterConfigKey]
        //static ModConfigurationKey<string> Key_TestString = new ModConfigurationKey<string>("Key_TestString", "Key_TestString", () => "TestStringOwo");

        static ModConfiguration config;

		static string WIZARD_TITLE = "Component Field Manipulator (Mod)";

		static string wizardActionString; // dynamically generated with the system time

		static string modReloadString = "Reload SyncMemberManipulator";

        //static ResoniteMod mod;

		// Very important method which is the entry point for hot-reloading
		// This should be called after the previous instance of the mod has been unloaded
		// And all relevant information has been transferred to the new assembly
		static void OnHotReload(ResoniteModBase mod)
		{
            Msg("In OnHotReload!");
			config = mod.GetConfiguration();
			//Msg("Test string: " + config.GetValue(Key_TestString));
			Setup();
		}

		static void Unload()
		{
			object categoryNode = AccessTools.Field(typeof(DevCreateNewForm), "root").GetValue(null);
			object subcategory = AccessTools.Method(categoryNode.GetType(), "GetSubcategory").Invoke(categoryNode, new object[] { "Editor" });
            System.Collections.IList elements = (System.Collections.IList)AccessTools.Field(categoryNode.GetType(), "_elements").GetValue(subcategory);
			if (elements == null)
			{
				Msg("Elements is null!");
				return;
			}
            foreach (object categoryItem in elements)
            {
                var name = (string)AccessTools.Field(categoryNode.GetType().GetGenericArguments()[0], "name").GetValue(categoryItem);
                //var action = (Action<Slot>)AccessTools.Field(categoryItemType, "action").GetValue(categoryItem);
                if (name == wizardActionString)
                {
                    elements.Remove(categoryItem);
                    break;
                }
            }
            foreach (object categoryItem in elements)
            {
                var name = (string)AccessTools.Field(categoryNode.GetType().GetGenericArguments()[0], "name").GetValue(categoryItem);
                //var action = (Action<Slot>)AccessTools.Field(categoryItemType, "action").GetValue(categoryItem);
                if (name == modReloadString)
                {
                    elements.Remove(categoryItem);
                    break;
                }
            }
        }

		static ResoniteModBase GetMod()
		{
			foreach (ResoniteModBase mod in ModLoader.Mods())
			{
				if (mod.GetType().Name == typeof(SyncMemberManipulatorMod).Name)
				{
					return mod;
				}
			}
			return null;
		}

		public override void OnEngineInit()
		{
            config = GetConfiguration();
            Engine.Current.RunPostInit(Setup);
		}

		static void AddMenuOption()
		{
            DateTime utcNow = DateTime.UtcNow;
			wizardActionString = WIZARD_TITLE + utcNow.ToString();
            DevCreateNewForm.AddAction("Editor", wizardActionString, (slot) => SyncMemberManipulator.CreateWizard(slot));
		}

		static void Setup()
		{
			AddMenuOption();
			Msg("Added menu option.");
            DevCreateNewForm.AddAction("Editor", modReloadString, (x) =>
            {
				x.Destroy();

                Msg("Reload button pressed.");
                Msg("Unloading mod...");

				// Basically does the opposite of what the mod does when it loads
				// Implemented by mod developer
                Unload();

                Msg("Loading the new assembly...");

                string dllPath = "G:\\SteamLibrary\\steamapps\\common\\Resonite\\rml_mods\\HotReloadMods\\SyncMemberManipulator.dll";
				var assemblyDefinition = AssemblyDefinition.ReadAssembly(dllPath);
                assemblyDefinition.Name.Name += "-" + DateTime.Now.Ticks.ToString();
				var memoryStream = new MemoryStream();
                assemblyDefinition.Write(memoryStream);
                Assembly assembly = Assembly.Load(memoryStream.ToArray());

                Msg("Loaded assembly: " + assembly.FullName);

                Type targetType = null;
                foreach (Type type in assembly.GetTypes())
                {
                    // The name of the ResoniteMod type 
                    if (type.Name.StartsWith(typeof(SyncMemberManipulatorMod).Name))
                    {
                        Msg("Found ResoniteMod type: " + type.Name);
                        targetType = type;
                        break;
                    }
                }

                if (targetType != null)
                {
                    // Transfer the instance of ResoniteMod to the new assembly
                    //AccessTools.Field(targetType, "mod").SetValue(null, mod);
                    MethodInfo method = AccessTools.Method(targetType, "OnHotReload");
                    if (method != null)
                    {
                        Msg("Invoking OnHotReload method...");
						//ResoniteModBase mod = GetMod();
						//mod.GetConfiguration().
						method.Invoke(null, new object[] { GetMod() });
                    }
                    else
                    {
                        Error("OnHotReload method is null!");
                    }
                }
                else
                {
                    Error("ResoniteMod type is null!");
                }
            });
			Msg("Reload action added.");
        }

        public class SyncMemberManipulator
        {
			public static SyncMemberManipulator CreateWizard(Slot x)
			{
				return new SyncMemberManipulator(x);
			}

			Slot WizardSlot;
			Slot WizardStaticContentSlot;
			Slot WizardGeneratedFieldsSlot;
			RectTransform WizardStaticContentRect;
			RectTransform WizardGeneratedFieldsRect;
			Slot WizardGeneratedContentSlot;
			RectTransform WizardGeneratedContentRect;
			Slot WizardSearchDataSlot;
			Slot WizardGeneratedFieldsDataSlot;
			UIBuilder WizardUI;

			ReferenceField<Slot> searchRoot;
			ReferenceField<Component> sourceComponent;

			struct SyncMemberWizardFields
			{
				public ISyncMember sourceSyncMember; // the syncMember to copy from
				public IField<bool> enabledField; // the checkbox that determines if the syncMember should be copied out to other components
			}

			// <workerName, <memberName, SyncMemberWizardFields>>
			Dictionary<string, Dictionary<string, SyncMemberWizardFields>> workerMemberFields = new Dictionary<string, Dictionary<string, SyncMemberWizardFields>>();

			const float CANVAS_WIDTH_DEFAULT = 800f; // 800f
			const float CANVAS_HEIGHT_DEFAULT = 1200f;

			SyncMemberManipulator(Slot x)
			{
                WizardSlot = x;
				WizardSlot.Tag = "Developer";
				WizardSlot.PersistentSelf = false;
				WizardSlot.LocalScale *= 0.0006f;

				WizardSearchDataSlot = WizardSlot.AddSlot("SearchData");
				WizardGeneratedFieldsDataSlot = WizardSlot.AddSlot("FieldsData");

				WizardUI = RadiantUI_Panel.SetupPanel(WizardSlot, WIZARD_TITLE.AsLocaleKey(), new float2(CANVAS_WIDTH_DEFAULT, CANVAS_HEIGHT_DEFAULT));
				RadiantUI_Constants.SetupEditorStyle(WizardUI);

				WizardUI.Canvas.MarkDeveloper();
				WizardUI.Canvas.AcceptPhysicalTouch.Value = false;

				WizardUI.Style.MinWidth = config.GetValue(Key_InitialMinWidth);
				WizardUI.Style.MinHeight = config.GetValue(Key_InitialMinHeight);
				WizardUI.Style.PreferredWidth = config.GetValue(Key_InitialPreferredWidth);
				WizardUI.Style.PreferredHeight = config.GetValue(Key_InitialPreferredHeight);
				WizardUI.Style.FlexibleWidth = config.GetValue(Key_InitialFlexibleWidth);
				WizardUI.Style.FlexibleHeight = config.GetValue(Key_InitialFlexibleHeight);

				WizardSlot.PositionInFrontOfUser(float3.Backward, distance: 1f);

				WizardStaticContentSlot = WizardUI.Root;
				WizardStaticContentRect = WizardUI.CurrentRect;

				RegenerateWizardUI();
			}

			void SetEnabledFields(bool val)
			{
				foreach (Dictionary<string, SyncMemberWizardFields> dict in workerMemberFields.Values)
				{
					foreach (SyncMemberWizardFields fields in dict.Values)
					{
						fields.enabledField.Value = val;
					}
				}
			}

			void UpdateCanvasSize()
			{
				// I couldn't get this to work right
				// So now the canvas is constant size lol
				WizardUI.Canvas.Size.Value = new float2(CANVAS_WIDTH_DEFAULT, CANVAS_HEIGHT_DEFAULT);
				return;

				WizardSlot.RunInUpdates(30, () => 
				{
					// supposed to be the size of the canvas that is actually used
					// 80 is height of panel header. 24 is extra padding to make it stop scrolling when it doesn't need to
					float newY = 80 + 24 + 12;
					foreach(Slot childSlot in WizardStaticContentSlot.Children)
					{
						if (childSlot.GetComponent<VerticalLayout>() != null) continue;

						RectTransform rectTransform = childSlot.GetComponent<RectTransform>();
						if (rectTransform != null && !rectTransform.IsRemoved)
						{
							newY += rectTransform.LocalComputeRect.size.y;
						}
					}
					if (WizardGeneratedFieldsRect != null && !WizardGeneratedFieldsRect.IsRemoved)
					{
						newY += WizardGeneratedFieldsRect.LocalComputeRect.size.y;
						if (WizardGeneratedContentRect != null && !WizardGeneratedContentRect.IsRemoved)
						{
							newY += WizardGeneratedFieldsRect.LocalComputeRect.size.y - WizardGeneratedContentRect.LocalComputeRect.size.y;
						}
					}
					WizardUI.Canvas.Size.Value = new float2(CANVAS_WIDTH_DEFAULT, MathX.Min(newY, CANVAS_HEIGHT_DEFAULT));
				});
			}

			void RegenerateWizardUI()
			{
				WizardSearchDataSlot.DestroyChildren();
				WizardStaticContentSlot.DestroyChildren();
				//var rect = WizardContentSlot.GetComponent<RectTransform>();
				WizardUI.ForceNext = WizardStaticContentRect;
				WizardStaticContentSlot.RemoveAllComponents((Component c) => c != WizardStaticContentRect);

				searchRoot = WizardSearchDataSlot.FindChildOrAdd("searchRoot").GetComponentOrAttach<ReferenceField<Slot>>();
				searchRoot.Reference.Target = WizardSlot.World.RootSlot;
				sourceComponent = WizardSearchDataSlot.FindChildOrAdd("sourceComponent").GetComponentOrAttach<ReferenceField<Component>>();

				VerticalLayout verticalLayout = WizardUI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
				verticalLayout.ForceExpandHeight.Value = false;

				SyncMemberEditorBuilder.Build(searchRoot.Reference, "Hierarchy Root Slot", null, WizardUI);
				SyncMemberEditorBuilder.Build(sourceComponent.Reference, "Source Component", null, WizardUI);

				WizardUI.Spacer(24f);

				WizardUI.PushStyle();

				WizardUI.Style.MinWidth = -1f;
				WizardUI.Style.MinHeight = -1f;
				WizardUI.Style.PreferredWidth = -1f;
				WizardUI.Style.PreferredHeight = -1f;
				WizardUI.Style.FlexibleWidth = -1f;
				WizardUI.Style.FlexibleHeight = -1f;

				VerticalLayout verticalLayout2 = WizardUI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
				verticalLayout2.ForceExpandHeight.Value = false;
				
				WizardGeneratedContentSlot = WizardUI.Root;
				WizardGeneratedContentRect = WizardUI.CurrentRect;

				WizardUI.PopStyle();

				sourceComponent.Reference.Changed += (reference) => 
				{
					WizardGeneratedFieldsDataSlot.DestroyChildren();
					WizardGeneratedContentSlot.DestroyChildren();
					//WizardUI.ForceNext = WizardGeneratedFieldsRect;
					WizardUI.NestInto(WizardGeneratedContentSlot);
					//WizardGeneratedFieldsSlot.RemoveAllComponents((Component c) => c != WizardGeneratedFieldsRect);
					if (((ISyncRef)reference).Target != null)
					{
						WizardUI.PushStyle();

						WizardUI.Style.MinWidth = -1f;
						WizardUI.Style.MinHeight = -1f;
						WizardUI.Style.PreferredWidth = -1f;
						WizardUI.Style.PreferredHeight = -1f;
						WizardUI.Style.FlexibleWidth = -1f;
						WizardUI.Style.FlexibleHeight = -1f;

						WizardUI.PushStyle();
						WizardUI.Style.FlexibleHeight = 1f;
						WizardUI.ScrollArea();
						WizardUI.FitContent(SizeFit.Disabled, SizeFit.PreferredSize);
						WizardUI.PopStyle();

						VerticalLayout fieldsVerticalLayout = WizardUI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
						fieldsVerticalLayout.ForceExpandHeight.Value = false;

						WizardGeneratedFieldsSlot = WizardUI.Root;
						WizardGeneratedFieldsRect = WizardUI.CurrentRect;

						WizardUI.PopStyle();

						WizardUI.Text("Component Fields");
                        WizardUI.Text("Changes made here will only be applied after clicking the apply button!");
						WizardUI.Spacer(24f);
						WizardUI.Button("Select All").LocalPressed += (btn, data) => 
						{
							SetEnabledFields(true);
						};
						WizardUI.Button("Deselect All").LocalPressed += (btn, data) =>
						{
							SetEnabledFields(false);
						};
						WizardUI.Spacer(24f);

						WizardUI.PushStyle();

						WizardUI.Style.MinWidth = config.GetValue(Key_FieldsMinWidth);
						WizardUI.Style.MinHeight = config.GetValue(Key_FieldsMinHeight);
						WizardUI.Style.PreferredWidth = config.GetValue(Key_FieldsPreferredWidth);
						WizardUI.Style.PreferredHeight = config.GetValue(Key_FieldsPreferredHeight);
						WizardUI.Style.FlexibleWidth = config.GetValue(Key_FieldsFlexibleWidth);
						WizardUI.Style.FlexibleHeight = config.GetValue(Key_FieldsFlexibleHeight);

						workerMemberFields.Clear();

						//dummyComponent = WizardGeneratedFieldsDataSlot.AddSlot(searchComponent.Reference.Target.Name).AttachComponent(searchComponent.Reference.Target.GetType());

						GenerateWorkerMemberEditors(WizardUI, sourceComponent.Reference.Target);

						WizardUI.PopStyle();

						WizardUI.NestOut(); // Out of GeneratedFieldsSlot, Into ScrollArea slot
						WizardUI.NestOut(); // Out of ScrollArea slot, Into WizardGeneratedContentSlot

						WizardUI.Spacer(24f);
						WizardUI.Button("Apply to Hierarchy (Undoable)").LocalPressed += (btn, data) => 
						{
							Msg("Apply pressed");
							Apply();
						};

						WizardUI.Spacer(24f);
					}
					UpdateCanvasSize();
				};
				UpdateCanvasSize();
			}

			private void HandleWorker(Worker worker)
			{
				if (!workerMemberFields.ContainsKey(worker.Name))
				{
					Warn($"Worker: {worker.Name}:{worker.GetType().GetNiceName()} does not exist in workerMemberFields");
					return;
				}
				foreach (ISyncMember syncMember in worker.SyncMembers)
				{
					Msg("syncMember Name: " + syncMember.Name);
					
					if (syncMember is SyncObject)
					{
						Msg("Is SyncObject");
						HandleWorker((Worker)syncMember);
					}
					else if (syncMember is IField)
					{
						if (!workerMemberFields[worker.Name].ContainsKey(syncMember.Name))
						{
							Warn("syncMember not in dictionary. Skipping.");
							continue;
						}
						// assume its a field because thats all that is supported here right now
						Msg("Is field");
						IField field = ((IField)syncMember);
						SyncMemberWizardFields fieldsStruct = workerMemberFields[worker.Name][syncMember.Name];
						if (fieldsStruct.enabledField.Value == false) continue;
						field.CreateUndoPoint();
						IField sourceMember = (IField)fieldsStruct.sourceSyncMember;
						field.BoxedValue = sourceMember.BoxedValue;
					}
					else
					{
						// lists etc unsupported for now
					}
				}
			}

			private void Apply()
			{
				if (searchRoot.Reference.Target == null || sourceComponent.Reference.Target == null) return;

				//if (workerMemberFields.Count == 0 || workerMemberFields.Values.Count == 0) return;

				WizardSlot.World.BeginUndoBatch("Set component fields");

				foreach(Component c in searchRoot.Reference.Target.GetComponentsInChildren((Component c) => 
					c.GetType() == sourceComponent.Reference.Target.GetType() && c != sourceComponent.Reference.Target))
				{
					Msg(c.Name);
					HandleWorker(c);
				}

				WizardSlot.World.EndUndoBatch();
			}

			void GenerateWorkerMemberEditors(UIBuilder UI, Worker targetWorker, bool recursive = true)
			{
				workerMemberFields.Add(targetWorker.Name, new Dictionary<string, SyncMemberWizardFields>());

				int i = -1;
				foreach (ISyncMember syncMember in targetWorker.SyncMembers)
				{
					i += 1;

					Type type = null;

					if (syncMember is IField field)
					{
						type = field.ValueType;
					}
					else if (syncMember is ISyncRef syncRef)
					{
						type = syncRef.TargetType;
					}
					else
					{
						type = syncMember.GetType();
					}

					colorX c = type.GetTypeColor().MulRGB(1.5f);
                    UI.Style.TextColor = MathX.LerpUnclamped(RadiantUI_Constants.TEXT_COLOR, c, 0.1f);
					//UI.Style.TextColor = RandomX.Hue;

                    if (syncMember is SyncObject)
					{
						UI.PushStyle();
						UI.Style.PreferredHeight = 24f;
						UI.Text($"{syncMember.Name}").HorizontalAlign.Value = TextHorizontalAlignment.Left;
						UI.PopStyle();
						if (recursive)
						{
							UI.PushStyle();
							UI.HorizontalLayout();
							UI.FitContent(SizeFit.Disabled, SizeFit.PreferredSize);
							UI.Style.MinWidth = 16f;
							UI.Style.FlexibleWidth = -1;
							UI.Panel();
							colorX color = colorX.Black;
							UI.Image(in color);
							UI.CurrentRect.OffsetMin.Value = new float2(UI.Style.MinWidth * 0.5f - 2f);
							UI.CurrentRect.OffsetMax.Value = new float2(0f - (UI.Style.MinWidth * 0.5f - 2f));
							UI.NestOut();

							VerticalLayout verticalLayout = UI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
							verticalLayout.ForceExpandHeight.Value = false;
							verticalLayout.PaddingLeft.Value = 6f;

							UI.PopStyle();

							GenerateWorkerMemberEditors(UI, (Worker)syncMember);

							UI.NestOut();
							UI.NestOut();
                        }
                        continue;
                    }
					else if (!(syncMember is IField))
					{
                        UI.PushStyle();
                        UI.Style.PreferredHeight = 24f;
						UI.Style.TextColor = colorX.Gray;
                        UI.Text($"{syncMember.Name} (not supported)").HorizontalAlign.Value = TextHorizontalAlignment.Left;
                        UI.PopStyle();
						continue;
                    }

					//Type genericTypeDefinition = null;
					//if (syncMember.GetType().IsGenericType)
					//{
					//	genericTypeDefinition = syncMember.GetType().GetGenericTypeDefinition();
					//}

					FieldInfo fieldInfo = targetWorker.GetSyncMemberFieldInfo(syncMember.Name);

					//Slot s = WizardGeneratedFieldsDataSlot.FindChildOrAdd(targetWorker.Name + ":" + i.ToString() + "_" + syncMember.Name);

					var horizontalLayout = UI.HorizontalLayout(4f, childAlignment: Alignment.MiddleLeft);
                    horizontalLayout.ForceExpandWidth.Value = false;
					//horizontalLayout.PaddingLeft.Value = CANVAS_WIDTH_DEFAULT / 4f;


                    UI.PushStyle();

					WizardUI.Style.MinWidth = config.GetValue(Key_CheckboxMinWidth);
					WizardUI.Style.MinHeight = config.GetValue(Key_CheckboxMinHeight);
					WizardUI.Style.PreferredWidth = config.GetValue(Key_CheckboxPreferredWidth);
					WizardUI.Style.PreferredHeight = config.GetValue(Key_CheckboxPreferredHeight);
					WizardUI.Style.FlexibleWidth = config.GetValue(Key_CheckboxFlexibleWidth);
					WizardUI.Style.FlexibleHeight = config.GetValue(Key_CheckboxFlexibleHeight);

					var checkbox = UI.Checkbox(false);

					UI.PopStyle();

					//SyncMemberEditorBuilder.Build(syncMember, syncMember.Name, fieldInfo, UI);

					UI.PushStyle();
                    UI.Style.PreferredHeight = 24f;
                    UI.Text($"{syncMember.Name}").HorizontalAlign.Value = TextHorizontalAlignment.Left;
					UI.PopStyle();

					//UI.MemberEditor((IField)syncMember, )

					SyncMemberWizardFields fieldsStruct = new SyncMemberWizardFields();
					fieldsStruct.sourceSyncMember = syncMember;
					fieldsStruct.enabledField = checkbox.State;

					workerMemberFields[targetWorker.Name].Add(syncMember.Name, fieldsStruct);

					UI.NestOut();
				}
			}
		}
	}
}