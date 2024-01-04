using System.Collections.Generic;
using ResoniteModLoader;
using FrooxEngine;
using FrooxEngine.UIX;
using Elements.Core;
using System;
using System.Reflection;
using HarmonyLib;
using FrooxEngine.Undo;
using System.Threading.Tasks;
using Elements.Assets;

namespace SyncMemberManipulator
{
	public class SyncMemberManipulatorMod : ResoniteMod
	{
		public override string Name => "SyncMemberManipulator";
		public override string Author => "Nytra";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/Nytra/ResoniteSyncMemberManipulator";

		const string WIZARD_TITLE = "Component Field Manipulator (Mod)";

		public override void OnEngineInit()
		{
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

			//private static FieldInfo rootsField = AccessTools.Field(typeof(UIBuilder), "roots");
			//private static FieldInfo uiStylesField = AccessTools.Field(typeof(UIBuilder), "_uiStyles");
			//private static FieldInfo currentField = AccessTools.Field(typeof(UIBuilder), "Current");

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

				WizardUI = RadiantUI_Panel.SetupPanel(WizardSlot, WIZARD_TITLE.AsLocaleKey(), new float2(800f, 1500f));
				RadiantUI_Constants.SetupEditorStyle(WizardUI);

				WizardUI.Canvas.MarkDeveloper();
				WizardUI.Canvas.AcceptPhysicalTouch.Value = false;

				WizardUI.Style.MinHeight = 24f;
				WizardUI.Style.PreferredHeight = 24f;
				WizardUI.Style.PreferredWidth = 96f;
				WizardUI.Style.MinWidth = 400f;

				WizardSlot.PositionInFrontOfUser(float3.Backward, distance: 1f);

				WizardContentSlot = WizardUI.Root;
				WizardContentRect = WizardUI.CurrentRect;

				//WizardStyle = WizardUI.Style.Clone();

				RegenerateWizardUI();
			}

			void SetEnabledFields(bool val)
			{
                foreach (SyncMemberWizardFields fieldsStruct in syncMemberNameFields.Values)
                {
                    fieldsStruct.enabledField.Value = val;
                }
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

				VerticalLayout verticalLayout2 = WizardUI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
				verticalLayout2.ForceExpandHeight.Value = false;

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
                        GenerateComponentMemberEditors(WizardUI, searchComponent.Reference.Target);
						WizardUI.Spacer(24f);
						WizardUI.Button("Apply to Hierarchy").LocalPressed += (btn, data) => 
						{
							Apply();
						};
					}
				};

				//if (!ValidateCurrentBuilder())
				//{
				//// build initial screen

				////WizardUI.PushStyle();

				//panelName = WizardDataSlot.FindChildOrAdd("Panel Name").GetComponentOrAttach<ValueField<string>>();
				//panelName.Value.Value = "Test UIX Panel";
				//panelSize = WizardDataSlot.FindChildOrAdd("Panel Size").GetComponentOrAttach<ValueField<float2>>();
				//panelSize.Value.Value = new float2(800f, 800f);

				//VerticalLayout verticalLayout = WizardUI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
				//verticalLayout.ForceExpandHeight.Value = false;

				//SyncMemberEditorBuilder.Build(panelName.Value, "Panel Name", null, WizardUI);
				//SyncMemberEditorBuilder.Build(panelSize.Value, "Panel Size", null, WizardUI);

				////GenerateStyleMemberEditors(WizardUI, WizardUI.Style);

				//WizardUI.Spacer(24f);

				//createPanelButton = WizardUI.Button("Create Panel");
				//createPanelButton.LocalPressed += (btn, data) =>
				//{
				//    Slot root = WizardSlot.LocalUserSpace.AddSlot(panelName.Value.Value);
				//    currentBuilder = CreatePanel(root, root.Name, panelSize.Value.Value);
				//    currentBuilder.Root.OnPrepareDestroy += (slot) =>
				//    {
				//        // Run an empty action after the slot gets destroyed simply to update the wizard UI
				//        WizardSlot.RunSynchronously(() =>
				//        {
				//            WizardAction(null, new ButtonEventData(), () => { });
				//        });
				//    };
				//    CopyStyle(WizardUI, currentBuilder);
				//    //WizardUI.PopStyle();
				//    RegenerateWizardUI();
				//};
				//}
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

			//void UpdateTexts()
			//{
			//    var roots = (Stack<Slot>)rootsField.GetValue(currentBuilder);
			//    var uiStyles = (Stack<UIStyle>)uiStylesField.GetValue(currentBuilder);

			//    rootText.Content.Value = $"[{roots.Count}] Root: ";
			//    if (IsSlotValid(currentBuilder.Root))
			//    {
			//        rootText.Content.Value += currentBuilder.Root.Name;
			//    }
			//    else
			//    {
			//        rootText.Content.Value += "Null";
			//    }
			//    currentText.Content.Value = "Current: ";
			//    if (IsSlotValid(currentBuilder.Current))
			//    {
			//        currentText.Content.Value += currentBuilder.Current.Name;
			//    }
			//    else
			//    {
			//        currentText.Content.Value += "Null";
			//    }
			//    //styleText.Content.Value = "Styles count: " + uiStyles.Count;
			//}

			//bool IsSlotValid(Slot s)
			//{
			//	if (s == null || s.IsRemoved)
			//	{
			//		return false;
			//	}
			//	return true;
			//}

			//bool ValidateCurrentBuilder()
			//{
			//    if (currentBuilder == null ||
			//        currentBuilder.Canvas == null ||
			//        !IsSlotValid(currentBuilder.Canvas.Slot) ||
			//        (!IsSlotValid(currentBuilder.Root) && !IsSlotValid(currentBuilder.Current)))
			//    {
			//        return false;
			//    }
			//    return true;
			//}

			//UIBuilder CreatePanel(Slot root, string name, float2 size)
			//{
			//	UIBuilder builder = RadiantUI_Panel.SetupPanel(root, name.AsLocaleKey(), size);
			//	RadiantUI_Constants.SetupEditorStyle(builder);
			//	root.LocalScale *= 0.0005f;
			//	root.PositionInFrontOfUser(float3.Backward, distance: 1f);
			//	return builder;
			//}

			//void CopyStyle(UIBuilder b1, UIBuilder b2)
			//{
			//    FieldInfo[] fields = typeof(UIStyle).GetFields();
			//    int i = 0;
			//    foreach (FieldInfo field in fields)
			//    {
			//        field.SetValue(b2.Style, field.GetValue(b1.Style));
			//        i++;
			//    }
			//}

			void GenerateComponentMemberEditors(UIBuilder UI, Component targetComponent)
			{
				syncMemberNameFields.Clear();

				int i = -1;
				foreach (ISyncMember syncMember in targetComponent.SyncMembers)
				{
					i += 1;

					// This will only work for fields, not things like lists or bags
					if (!(syncMember is IField)) continue;

					Type genericTypeDefinition = null;
					if (syncMember.GetType().IsGenericType)
					{
						genericTypeDefinition = syncMember.GetType().GetGenericTypeDefinition();
					}

					FieldInfo fieldInfo = targetComponent.GetSyncMemberFieldInfo(syncMember.Name);

					Type t = null;
					string memberName = null;

					if (genericTypeDefinition == typeof(Sync<>))
					{
						t = typeof(ValueField<>).MakeGenericType(((IField)syncMember).ValueType);
						memberName = "Value";
					}
					else if (genericTypeDefinition == typeof(SyncRef<>))
					{
						t = typeof(ReferenceField<>).MakeGenericType(((ISyncRef)syncMember).TargetType);
						memberName = "Reference";
					}
					else if (genericTypeDefinition == typeof(SyncDelegate<>))
					{
						t = typeof(DelegateTag<>).MakeGenericType(((ISyncRef)syncMember).TargetType);
						memberName = "Delegate";
					}
					else if (syncMember is SyncType)
					{
						t = typeof(TypeField);
						memberName = "Type";
					}

					if (t == null || memberName == null) continue;

					Slot s = WizardGeneratedFieldsDataSlot.FindChildOrAdd(i.ToString() + "_" + syncMember.Name);

                    //UI.Style.PreferredWidth = 96f;
                    //UI.Style.MinWidth = 400f;
                    
					UI.PushStyle();

                    UI.Style.FlexibleWidth = -1f;

                    UI.HorizontalLayout(4f, childAlignment: Alignment.MiddleLeft).ForceExpandWidth.Value = false;

                    //UI.Style.PreferredWidth = 48f;
                    //UI.Style.MinWidth = 48f;

                    var checkbox = UI.Checkbox(false);

                    //UI.Style.PreferredWidth = 400f;
                    UI.Style.MinWidth = 724f;
					//UI.Style.FlexibleWidth = 400f;

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

				UI.PopStyle();
            }

			//void CreatePanelWithMethodParameters(MethodInfo method)
			//{
			//    Slot root = WizardSlot.LocalUserSpace.AddSlot("Test Panel with Args");
			//    UIBuilder UI = CreatePanel(root, root.Name, new float2(800, 800));

			//    VerticalLayout verticalLayout = UI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
			//    verticalLayout.ForceExpandHeight.Value = false;

			//    ParameterInfo[] parameters = method.GetParameters();
			//    int i = 0;
			//    foreach (ParameterInfo param in parameters)
			//    {
			//        Slot s = WizardDataSlot.FindChildOrAdd(i.ToString() + "_" + param.Name);
			//        if (param.ParameterType.IsValueType)
			//        {
			//            Type t = typeof(ValueField<>).MakeGenericType(param.ParameterType);
			//            Component c = s.AttachComponent(t);
			//            SyncMemberEditorBuilder.Build(c.GetSyncMember("Value"), param.Name, null, UI);
			//        }
			//        else
			//        {
			//            Type t = typeof(ReferenceField<>).MakeGenericType(param.ParameterType);
			//            Component c = s.AttachComponent(t);
			//            SyncMemberEditorBuilder.Build(c.GetSyncMember("Reference"), param.Name, null, UI);
			//        }
			//        i++;
			//    }
			//}

			// ===== ACTIONS =====

			//void EditStyle(IButton button, ButtonEventData eventData)
			//{
			//    WizardAction(button, eventData, () =>
			//    {
			//        Slot s = WizardSlot.LocalUserSpace.AddSlot("Style Edit Panel");
			//        UIBuilder b = CreatePanel(s, s.Name, new float2(800, 1500));
			//        b.Canvas.MarkDeveloper();
			//        b.Canvas.AcceptPhysicalTouch.Value = false;
			//        VerticalLayout verticalLayout = b.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
			//        verticalLayout.ForceExpandHeight.Value = false;
			//        GenerateStyleMemberEditors(b, currentBuilder.Style);
			//    });
			//}
		}
	}
}