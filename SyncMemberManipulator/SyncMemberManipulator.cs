using System.Collections.Generic;
using ResoniteModLoader;
using FrooxEngine;
using FrooxEngine.UIX;
using Elements.Core;
using System;
using System.Reflection;
using FrooxEngine.Undo;
using HarmonyLib;

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
        static ModConfigurationKey<float> Key_CheckboxPreferredWidth = new ModConfigurationKey<float>("Key_CheckboxPreferredWidth", "Key_CheckboxPreferredWidth", () => 48f);
        [AutoRegisterConfigKey]
        static ModConfigurationKey<float> Key_CheckboxPreferredHeight = new ModConfigurationKey<float>("Key_CheckboxPreferredHeight", "Key_CheckboxPreferredHeight", () => -1f);
        [AutoRegisterConfigKey]
        static ModConfigurationKey<float> Key_CheckboxFlexibleWidth = new ModConfigurationKey<float>("Key_CheckboxFlexibleWidth", "Key_CheckboxFlexibleWidth", () => 1f);
        [AutoRegisterConfigKey]
        static ModConfigurationKey<float> Key_CheckboxFlexibleHeight = new ModConfigurationKey<float>("Key_CheckboxFlexibleHeight", "Key_CheckboxFlexibleHeight", () => -1f);

        static ModConfiguration config;

        const string WIZARD_TITLE = "Component Field Manipulator (Mod)";

		public override void OnEngineInit()
		{
			config = GetConfiguration();
			Engine.Current.RunPostInit(AddMenuOption);
		}
		void AddMenuOption()
		{
			DevCreateNewForm.AddAction("Editor", WIZARD_TITLE, (x) => SyncMemberManipulator.CreateWizard(x));
		}

		class SyncMemberManipulator
		{
			public static SyncMemberManipulator CreateWizard(Slot x)
			{
				return new SyncMemberManipulator(x);
			}

			Slot WizardSlot;
			Slot WizardContentSlot;
			Slot WizardGeneratedFieldsSlot;
			RectTransform WizardContentRect;
			RectTransform WizardGeneratedFieldsRect;
			Slot WizardSearchDataSlot;
			Slot WizardGeneratedFieldsDataSlot;
			UIBuilder WizardUI;

			ReferenceField<Slot> searchRoot;
			ReferenceField<Component> searchComponent;

			struct SyncMemberWizardFields
			{
				public IField editorField; // the field that the user types the value into
				public IField<bool> enabledField; // the checkbox that determines if the field value should be copied out to other components
			}

			Dictionary<string, SyncMemberWizardFields> syncMemberNameFields = new Dictionary<string, SyncMemberWizardFields>();

			SyncMemberManipulator(Slot x)
			{
				WizardSlot = x;
				WizardSlot.Tag = "Developer";
				WizardSlot.PersistentSelf = false;
				WizardSlot.LocalScale *= 0.0006f;

				WizardSearchDataSlot = WizardSlot.AddSlot("SearchData");
				WizardGeneratedFieldsDataSlot = WizardSlot.AddSlot("FieldsData");

				WizardUI = RadiantUI_Panel.SetupPanel(WizardSlot, WIZARD_TITLE.AsLocaleKey(), new float2(800, 800));
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

				WizardContentSlot = WizardUI.Root;
				WizardContentRect = WizardUI.CurrentRect;

				RegenerateWizardUI();
			}

			void SetEnabledFields(bool val)
			{
                foreach (SyncMemberWizardFields fieldsStruct in syncMemberNameFields.Values)
                {
                    fieldsStruct.enabledField.Value = val;
                }
            }

			void UpdateCanvasSize()
			{
				WizardSlot.RunInUpdates(3, () => 
				{
					// hardcoded magic numbers oof
					// supposed to be the size of the canvas that is actually used
					// 24 is preferred height of the default elements. There are 4. 5? 24*4. 80 is height on panel header. 12 is extra to make it look nice?
					float newY = WizardGeneratedFieldsRect.LocalComputeRect.size.y + 24 * 4 + 80;// + 12;
                    WizardUI.Canvas.Size.Value = new float2(800, newY);
                });
            }

			void RegenerateWizardUI()
			{
				WizardSearchDataSlot.DestroyChildren();
				WizardContentSlot.DestroyChildren();
				//var rect = WizardContentSlot.GetComponent<RectTransform>();
				WizardUI.ForceNext = WizardContentRect;
				WizardContentSlot.RemoveAllComponents((Component c) => c != WizardContentRect);

				searchRoot = WizardSearchDataSlot.FindChildOrAdd("searchRoot").GetComponentOrAttach<ReferenceField<Slot>>();
				searchRoot.Reference.Target = WizardSlot.World.RootSlot;
				searchComponent = WizardSearchDataSlot.FindChildOrAdd("searchComponent").GetComponentOrAttach<ReferenceField<Component>>();

				VerticalLayout verticalLayout = WizardUI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
				verticalLayout.ForceExpandHeight.Value = false;

				SyncMemberEditorBuilder.Build(searchRoot.Reference, "Search Root", null, WizardUI);
				SyncMemberEditorBuilder.Build(searchComponent.Reference, "Component Type", null, WizardUI);

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

                WizardUI.FitContent(SizeFit.Disabled, SizeFit.PreferredSize);

                WizardUI.PopStyle();

                WizardGeneratedFieldsSlot = WizardUI.Root;
				WizardGeneratedFieldsRect = WizardUI.CurrentRect;

				searchComponent.Reference.Changed += (reference) => 
				{
					WizardGeneratedFieldsDataSlot.DestroyChildren();
					WizardGeneratedFieldsSlot.DestroyChildren();
                    //WizardUI.ForceNext = WizardGeneratedFieldsRect;
                    WizardUI.NestInto(WizardGeneratedFieldsRect);
					//WizardGeneratedFieldsSlot.RemoveAllComponents((Component c) => c != WizardGeneratedFieldsRect);
					if (((ISyncRef)reference).Target != null)
					{
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

                        GenerateComponentMemberEditors(WizardUI, searchComponent.Reference.Target);

                        WizardUI.PopStyle();

                        WizardUI.Spacer(24f);
                        WizardUI.Button("Apply to Hierarchy (Undoable)").LocalPressed += (btn, data) => 
						{
							Apply();
						};
						WizardUI.Spacer(24f);
					}
					UpdateCanvasSize();
                };
				UpdateCanvasSize();
			}

			private void Apply()
			{
				if (searchRoot.Reference.Target == null || searchComponent.Reference.Target == null) return;

				WizardSlot.World.BeginUndoBatch("Set component fields");

				foreach(Component c in searchRoot.Reference.Target.GetComponentsInChildren((Component c) => c.GetType() == searchComponent.Reference.Target.GetType()))
				{
					foreach(string key in syncMemberNameFields.Keys)
					{
						ISyncMember syncMember = c.GetSyncMember(key);
                        IField field = ((IField)syncMember);
                        SyncMemberWizardFields fieldsStruct = syncMemberNameFields[syncMember.Name];
                        if (fieldsStruct.enabledField.Value == false) continue;
                        if (field.BoxedValue == fieldsStruct.editorField.BoxedValue) continue;
                        field.CreateUndoPoint();
                        field.BoxedValue = fieldsStruct.editorField.BoxedValue;
                    }
				}

				WizardSlot.World.EndUndoBatch();
			}

			void GenerateComponentMemberEditors(UIBuilder UI, Component targetComponent)
			{
				syncMemberNameFields.Clear();

				int i = -1;
				foreach (ISyncMember syncMember in targetComponent.SyncMembers)
				{
					i += 1;

					// This will only work for fields, not things like lists or bags
					if (!(syncMember is IField)) 
					{
						UI.PushStyle();
						UI.Style.PreferredHeight = 24f;
						UI.Text($"<color=gray>{syncMember.Name}:{syncMember.GetType().GetNiceName()} (not supported)</color>");
						UI.PopStyle();
						continue;
					};

					Type genericTypeDefinition = null;
					if (syncMember.GetType().IsGenericType)
					{
						genericTypeDefinition = syncMember.GetType().GetGenericTypeDefinition();
					}

					FieldInfo fieldInfo = targetComponent.GetSyncMemberFieldInfo(syncMember.Name);

					Type t = null;
					string memberName = null;

					try
					{
                        if (genericTypeDefinition == typeof(Sync<>))
                        {
                            t = typeof(ValueField<>).MakeGenericType(((IField)syncMember).ValueType);
                            memberName = "Value";
                        }
                        else if (genericTypeDefinition == typeof(AssetRef<>))
                        {
                            t = typeof(AssetLoader<>).MakeGenericType(syncMember.GetType().GetGenericArguments()[0]);
                            memberName = "Asset";
                        }
                        else if (syncMember is SyncType)
                        {
                            t = typeof(TypeField);
                            memberName = "Type";
                        }
                        else if (genericTypeDefinition == typeof(SyncDelegate<>))
                        {
                            t = typeof(DelegateTag<>).MakeGenericType(((ISyncRef)syncMember).TargetType);
                            memberName = "Delegate";
                        }
                        else if (syncMember is ISyncRef)
                        {
                            t = typeof(ReferenceField<>).MakeGenericType(((ISyncRef)syncMember).TargetType);
                            memberName = "Reference";
                        }
                    }
					catch (Exception e)
					{
						Error(e.ToString());
                        UI.PushStyle();
                        UI.Style.PreferredHeight = 24f;
                        UI.Text($"<color=red>{syncMember.Name}:{syncMember.GetType().GetNiceName()} (threw exception)</color>");
						UI.PopStyle();
						continue;
					}
					

					if (t == null || memberName == null)
					{
                        UI.PushStyle();
                        UI.Style.PreferredHeight = 24f;
                        UI.Text($"<color=sub.purple>{syncMember.Name}:{syncMember.GetType().GetNiceName()} (should be supported?)</color>");
						UI.PopStyle();
                        continue;
                    };

					Slot s = WizardGeneratedFieldsDataSlot.FindChildOrAdd(i.ToString() + "_" + syncMember.Name);

                    UI.HorizontalLayout(4f, childAlignment: Alignment.MiddleLeft).ForceExpandWidth.Value = false;

                    UI.PushStyle();

                    WizardUI.Style.MinWidth = config.GetValue(Key_CheckboxMinWidth);
                    WizardUI.Style.MinHeight = config.GetValue(Key_CheckboxMinHeight);
                    WizardUI.Style.PreferredWidth = config.GetValue(Key_CheckboxPreferredWidth);
                    WizardUI.Style.PreferredHeight = config.GetValue(Key_CheckboxPreferredHeight);
                    WizardUI.Style.FlexibleWidth = config.GetValue(Key_CheckboxFlexibleWidth);
                    WizardUI.Style.FlexibleHeight = config.GetValue(Key_CheckboxFlexibleHeight);

                    var checkbox = UI.Checkbox(false);

                    UI.PopStyle();

                    Component c = s.GetComponent(t);
					bool subscribe = false;
					if (c == null)
					{
						c = s.AttachComponent(t);
						//subscribe = true;
					}
					ISyncMember generatedMember = c.GetSyncMember(memberName);
                    ((IField)generatedMember).BoxedValue = ((IField)syncMember).BoxedValue;
					SyncMemberEditorBuilder.Build(generatedMember, syncMember.Name, fieldInfo, UI);
					if (subscribe)
					{
						generatedMember.Changed += (value) => { ((IField)syncMember).BoxedValue = ((IField)value).BoxedValue; };
					}
					SyncMemberWizardFields fieldsStruct = new SyncMemberWizardFields();
                    fieldsStruct.editorField = (IField)generatedMember;
					fieldsStruct.enabledField = checkbox.State;
					syncMemberNameFields.Add(syncMember.Name, fieldsStruct);

                    UI.NestOut();
                }
            }
		}
	}
}