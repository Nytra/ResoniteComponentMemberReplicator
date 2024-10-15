using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using FrooxEngine.ProtoFlux.CoreNodes;
using FrooxEngine.UIX;
using FrooxEngine.Undo;
using HarmonyLib;
//using ResoniteHotReloadLib;
using ResoniteModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ComponentMemberReplicator
{
	public class ComponentMemberReplicatorMod : ResoniteMod
	{
		public override string Name => "ComponentMemberReplicator";
		public override string Author => "Nytra";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/Nytra/ResoniteComponentMemberReplicator";

		static ModConfiguration config;

		static string WIZARD_TITLE
		{
			get
			{
				string s = "Component Member Replicator (Mod)";
				//s += " " + HotReloader.GetReloadedCountOfModType(typeof(ComponentMemberReplicatorMod)).ToString();
				return s;
			}
		}

		public override void OnEngineInit()
		{
			config = GetConfiguration();
			//HotReloader.RegisterForHotReload(this);
			Engine.Current.RunPostInit(Setup);
		}

		//static void BeforeHotReload()
		//{
		//	Msg("In BeforeHotReload!");
		//	HotReloader.RemoveMenuOption("Editor", wizardActionString);
		//}

		//static void OnHotReload(ResoniteMod modInstance)
		//{
		//	Msg("In OnHotReload!");
		//	config = modInstance.GetConfiguration();
		//	Setup();
		//}

		static void AddMenuOption()
		{
			DevCreateNewForm.AddAction("Editor", WIZARD_TITLE, (slot) => ComponentMemberReplicator.CreateWizard(slot));
		}

		static void Setup()
		{
			AddMenuOption();
		}

		public class ComponentMemberReplicator
		{
			public static ComponentMemberReplicator CreateWizard(Slot x)
			{
				return new ComponentMemberReplicator(x);
			}

			Slot WizardSlot;
			Slot WizardStaticContentSlot;
			Slot WizardGeneratedContentSlot;
			Slot WizardSearchDataSlot;
			UIBuilder WizardUI;

			ReferenceField<Slot> searchRoot;
			ReferenceField<Component> sourceComponent;
			ReferenceField<Component> targetComponent;

			ValueField<int> modeField;

			bool subscribedToReferenceChanges = false;

			public enum ModeEnum
			{
				Write,
				DriveFromSource,
				CopyExistingDrivesFromSource,
				WriteOrCopyExistingDrivesFromSource
			}

			public ModeEnum Mode => (ModeEnum)Enum.GetValues(typeof(ModeEnum)).GetValue(modeField.Value);

			ValueField<bool> breakExistingDrives;

			bool DriveFromSource => Mode == ModeEnum.DriveFromSource;
			bool CopyExistingDrivesFromSource => Mode == ModeEnum.CopyExistingDrivesFromSource || Mode == ModeEnum.WriteOrCopyExistingDrivesFromSource;
			bool ShouldDrive => DriveFromSource || CopyExistingDrivesFromSource;

			bool ShouldWrite => Mode == ModeEnum.Write || Mode == ModeEnum.WriteOrCopyExistingDrivesFromSource;

			static Dictionary<Component, Component> newCompMappings = new Dictionary<Component, Component>();

			struct SyncMemberData
			{
				public ISyncMember sourceSyncMember; // the syncMember to copy from
				public IField<bool> enabledField; // the checkbox that determines if the syncMember should be copied out to other components
				// should add member stack here too probably
			}

			// workers with same name?
			// syncMembers with same name?
			// should check syncMemberIndex?

			// <workerName, <memberName, SyncMemberData>>
			// could break if there are nested workers with same name?
			Dictionary<string, Dictionary<string, SyncMemberData>> workerMemberFields = new Dictionary<string, Dictionary<string, SyncMemberData>>();

			const float CANVAS_WIDTH_DEFAULT = 800f; // 800f
			const float CANVAS_HEIGHT_DEFAULT = 1224f;

			ComponentMemberReplicator(Slot x)
			{
				WizardSlot = x;
				WizardSlot.Tag = "Developer";
				WizardSlot.PersistentSelf = false;
				WizardSlot.LocalScale *= 0.0006f;

				WizardSearchDataSlot = WizardSlot.AddSlot("SearchData");

				WizardUI = RadiantUI_Panel.SetupPanel(WizardSlot, WIZARD_TITLE.AsLocaleKey(), new float2(CANVAS_WIDTH_DEFAULT, CANVAS_HEIGHT_DEFAULT));
				RadiantUI_Constants.SetupEditorStyle(WizardUI);

				WizardUI.Canvas.MarkDeveloper();
				WizardUI.Canvas.AcceptPhysicalTouch.Value = false;

				WizardUI.Style.MinWidth = -1f;
				WizardUI.Style.MinHeight = 24f;
				WizardUI.Style.PreferredWidth = -1f;
				WizardUI.Style.PreferredHeight = 24f;
				WizardUI.Style.FlexibleWidth = -1f;
				WizardUI.Style.FlexibleHeight = -1f;

				WizardSlot.PositionInFrontOfUser(float3.Backward, distance: 1f);

				WizardStaticContentSlot = WizardUI.Root;

				RegenerateWizardUI();
			}

			void SetEnabledFields(bool val)
			{
				foreach (Dictionary<string, SyncMemberData> dict in workerMemberFields.Values)
				{
					foreach (SyncMemberData fields in dict.Values)
					{
						fields.enabledField.Value = val;
					}
				}
			}

			//void UpdateApplyButtonEnabled()
			//{
			//	bool b = false;
			//	foreach (Dictionary<string, SyncMemberWizardFields> dict in workerMemberFields.Values)
			//	{
			//		foreach (SyncMemberWizardFields fields in dict.Values)
			//		{
			//			if (fields.enabledField.Value == true)
			//			{
			//				b = true;
			//				break;
			//			}
			//		}
			//	}
			//	Button applyButton = null;
			//	if (applyButton.FilterWorldElement() != null)
			//	{
			//		applyButton.Enabled = b && searchRoot.Reference.Target != null;
			//	}
			//}

			void UpdateCanvasSize()
			{
				// I couldn't get this to work right
				// So now the canvas is constant size
				return;

				//WizardSlot.RunInUpdates(30, () => 
				//{
				//	// supposed to be the size of the canvas that is actually used
				//	// 80 is height of panel header. 24 is extra padding to make it stop scrolling when it doesn't need to
				//	float newY = 80 + 24 + 12;
				//	foreach(Slot childSlot in WizardStaticContentSlot.Children)
				//	{
				//		if (childSlot.GetComponent<VerticalLayout>() != null) continue;

				//		RectTransform rectTransform = childSlot.GetComponent<RectTransform>();
				//		if (rectTransform != null && !rectTransform.IsRemoved)
				//		{
				//			newY += rectTransform.LocalComputeRect.size.y;
				//		}
				//	}
				//	if (WizardGeneratedFieldsRect != null && !WizardGeneratedFieldsRect.IsRemoved)
				//	{
				//		newY += WizardGeneratedFieldsRect.LocalComputeRect.size.y;
				//		if (WizardGeneratedContentRect != null && !WizardGeneratedContentRect.IsRemoved)
				//		{
				//			newY += WizardGeneratedFieldsRect.LocalComputeRect.size.y - WizardGeneratedContentRect.LocalComputeRect.size.y;
				//		}
				//	}
				//	WizardUI.Canvas.Size.Value = new float2(CANVAS_WIDTH_DEFAULT, MathX.Min(newY, CANVAS_HEIGHT_DEFAULT));
				//});
			}

			IEnumerable<ISyncMember> EnumerateMembersRecursively(ISyncMember member)
			{
				if (member is Worker nextWorker)
				{
					foreach (var nextWorkerMember in EnumerateMembersRecursively(nextWorker))
					{
						yield return nextWorkerMember;
					}
				}
				else if (member is ISyncList list)
				{
					//Debug($"Found list: {list.Name}");
					var genericArg = list.GetType().GetGenericArguments()[0];
					if (typeof(Worker).IsAssignableFrom(genericArg))
					{
						//Debug("List of workers");
						foreach (var elem in list.Elements)
						{
							foreach (var listMember in EnumerateMembersRecursively((Worker)elem))
							{
								yield return listMember;
							}
						}
					}
					else if (typeof(ISyncMember).IsAssignableFrom(genericArg))
					{
						foreach (var elem in list.Elements)
						{
							yield return (ISyncMember)elem;
						}
					}
				}
				else if (member is ISyncBag bag)
				{
					//Debug($"Found bag: {bag.Name}");
					var genericArg = bag.GetType().GetGenericArguments()[0];
					if (typeof(Worker).IsAssignableFrom(genericArg))
					{
						//Debug("Bag of workers");
						foreach (var elem in bag.Values)
						{
							foreach (var bagMember in EnumerateMembersRecursively((Worker)elem))
							{
								yield return bagMember;
							}
						}
					}
					else if (typeof(ISyncMember).IsAssignableFrom(genericArg))
					{
						foreach (var elem in bag.Values)
						{
							yield return (ISyncMember)elem;
						}
					}
				}
			}

			IEnumerable<ISyncMember> EnumerateMembersRecursively(Worker rootWorker)
			{
				foreach (var member in rootWorker.SyncMembers)
				{
					yield return member;
					foreach (var allMember in EnumerateMembersRecursively(member))
					{
						yield return allMember;
					}
				}
			}

			class DriveData
			{
				public Stack<ISyncMember> stackToTargetMember;
				public ISyncMember targetMember = null;
			}

			Stack<ISyncMember> GetMemberStack(ISyncMember member)
			{
				var stack = new Stack<ISyncMember>();
				bool found = false;
				var currentMember = member;
				while (!found)
				{
					stack.Push(currentMember);
					if (currentMember.Parent is ISyncMember parentMember)
					{
						currentMember = parentMember;
					}
					else
					{
						found = true;
					}
				}
				return stack;
			}

			void CollectDriveData(Worker worker, List<DriveData> allDriveData)
			{
				foreach (var member in EnumerateMembersRecursively(worker))
				{
					if (member is IField && (DriveFromSource || (CopyExistingDrivesFromSource && member.IsDriven)))
					{
						var driveData = new DriveData();
						driveData.targetMember = member;
						driveData.stackToTargetMember = GetMemberStack(member);
						allDriveData.Add(driveData);
					}
				}
			}

			string ElementIdentifierString(IWorldElement elem)
			{
				return $"{elem.Name} {elem.ReferenceID}";
			}

			static ValueCopy<T> GetValueCopy<T>(IField fromField, IField toField)
			{
				return ((IField<T>)toField).DriveFrom((IField<T>)fromField);
			}

			static ReferenceCopy<T> GetReferenceCopy<T>(IField fromRef, IField toRef) where T : class, IWorldElement
			{
				return ((SyncRef<T>)toRef).DriveFrom((SyncRef<T>)fromRef);
			}

			void SafeFieldUndoPoint(IField field)
			{
				if (field.ValueType.IsEnginePrimitive() || field.ValueType == typeof(RefID) || field.ValueType == typeof(Type))
				{
					field.CreateUndoPoint();
				}
			}

			bool CopyDrives(SyncElement fromElement, SyncElement toElement, Dictionary<Component, Component> newCompMappings, bool undoable = false, bool recursive = false, ulong recursionDepth = 0)
			{
				var link = fromElement.ActiveLink;
				var comp = link.FindNearestParent<Component>();

				var targetComponent = toElement.FindNearestParent<Component>();
				var targetSlot = targetComponent.Slot;

				if (!breakExistingDrives.Value && toElement.IsDriven)
				{
					Debug("Target element is driven, skipping");
					return false;
				}

				Debug($"Recursion depth: {recursionDepth}");

				Debug($"Copying drive for element {ElementIdentifierString(toElement)} on component {ElementIdentifierString(targetComponent)} on slot {ElementIdentifierString(targetSlot)}");

				Debug($"Source field is driven by {ElementIdentifierString(link)} of type {link.GetType().GetNiceName()} on component {ElementIdentifierString(comp)}");

				if (comp is ProtoFluxEngineProxy proxy) // && proxy.GetSyncMember("Drive") is ILinkRef proxyLinkRef && proxyLinkRef.Target != null && typeof(IField).IsAssignableFrom(proxyLinkRef.Target.GetType()))
				{
					// ProtoFlux drive node

					Debug("Is ProtoFluxEngineProxy Drive");

					if (proxy.Node.Target is null)
					{
						Debug("Proxy node target is null, skipping");
						return false;
					}

					var origDriveNode = proxy.Node.Target.FindNearestParent<Component>();

					if (!(origDriveNode is IDrive))
					{
						Debug("Original drive node is not IDrive, probably is FieldHook node or similar, skipping");
						return false;
					}

					var newSlot = comp.Slot.Parent.AddSlot(comp.Slot.Name + "_duped");
					var dupedDriveNode = newSlot.DuplicateComponent(origDriveNode, breakExternalReferences: true);
					if (undoable)
					{
						newSlot.CreateSpawnUndoPoint();
					}
					if (recursive)
					{
						Debug("Entered recursive part.");

						var allDriveData = new List<DriveData>();
						CollectDriveData(origDriveNode, allDriveData);
						foreach (var driveData in allDriveData)
						{
							Debug($"Found driven member on source component that needs to be copied: {ElementIdentifierString(driveData.targetMember)}");
							var correspondingMember = FindCorrespondingMember(dupedDriveNode, driveData.targetMember, driveData.stackToTargetMember);
							if (CopyDrives((SyncElement)driveData.targetMember, (SyncElement)correspondingMember, newCompMappings, undoable: undoable, recursive: recursive, recursionDepth: recursionDepth + 1))
							{
								Debug("Copied drive.");
							}
							else
							{
								Debug("Failed to copy drive.");
							}
							Debug($"Recursion depth: {recursionDepth}");
						}

						Debug("Finished recursive part.");
					}

					if (undoable)
					{
						SafeFieldUndoPoint((IField)toElement);
					}

					if (dupedDriveNode is IDrive driveNode && driveNode.TrySetRootTarget(toElement))
					{
						Debug("ProtoFlux drive node copied.");
						return true;
					}
					//else if (dupedDriveNode is FrooxEngine.FrooxEngine.ProtoFlux.IProtoFluxEngineProxyNode proxyNode)
					//{
					//	// FieldHook Node

					//	Debug("dupedDriveNode is FieldHook node, the code shouldn't have gotten this far, skipping");
					//	return false;
					//}
					Debug("Failed to copy ProtoFlux drive node.");
					return false;
				}

				Debug($"Requires mapping for source component: {ElementIdentifierString(comp)}");

				Component newComp;
				bool newSpawn = false;
				if (newCompMappings.TryGetValue(comp, out var existingComp))
				{
					newComp = existingComp;
					Debug($"Found existing component mapping: {ElementIdentifierString(newComp)}");
				}
				else
				{
					// It breaks the original drive if I don't set breakExternalReferences to true
					newComp = targetSlot.DuplicateComponent(comp, breakExternalReferences: true);
					newCompMappings.Add(comp, newComp);
					Debug($"Duplicated new component, added new component mapping: {ElementIdentifierString(newComp)}");
					newSpawn = true;
				}

				// If it got here then it's either not driven or we should break the drive
				if (toElement.IsDriven)
				{
					if (breakExistingDrives.Value)
					{
						toElement.ActiveLink.ReleaseLink(undoable: undoable);
					}
				}
				else
				{
					if (undoable && toElement is IField toField)
					{
						SafeFieldUndoPoint(toField);
					}
				}
				if (newSpawn && undoable)
				{
					newComp.CreateSpawnUndoPoint();
				}

				var pathToLink = GetMemberStack(link);

				if (recursive)
				{
					Debug("Entered recursive part.");

					var allDriveData = new List<DriveData>();
					CollectDriveData(comp, allDriveData);
					foreach (var driveData in allDriveData)
					{
						Debug($"Found driven member on source component that needs to be copied: {ElementIdentifierString(driveData.targetMember)}");
						var correspondingMember = FindCorrespondingMember(newComp, driveData.targetMember, driveData.stackToTargetMember);
						if (CopyDrives((SyncElement)driveData.targetMember, (SyncElement)correspondingMember, newCompMappings, undoable: undoable, recursive: recursive, recursionDepth: recursionDepth + 1))
						{
							Debug("Copied drive.");
						}
						else
						{
							Debug("Failed to copy drive.");
						}
						Debug($"Recursion depth: {recursionDepth}");
					}

					Debug("Finished recursive part.");
				}

				ISyncMember foundMember = FindCorrespondingMember(newComp, link, pathToLink);

				if (foundMember != null && foundMember.Name == link.Name && foundMember.GetType() == link.GetType())
				{
					var syncRef = (ISyncRef)foundMember;
					syncRef.Target = toElement;
					return true;
				}

				return false;
			}

			ISyncMember FindCorrespondingMember(Worker root, ISyncMember memberToFind, Stack<ISyncMember> pathToMemberToFind)
			{
				var rootWorker = root;
				Debug($"Searching {rootWorker.Name} for {memberToFind.Name} of type {memberToFind.GetType().GetNiceName()}");
				Debug($"Current stack: {string.Join(",", pathToMemberToFind.Select(x => x.Name))}");
				var syncElementListType = typeof(SyncElementList<>).MakeGenericType(memberToFind.GetType());
				while (pathToMemberToFind.Count > 0)
				{
					var member = pathToMemberToFind.Pop();
					Debug($"Looking for: {member.Name}");
					var correspondingMember = rootWorker.GetSyncMember(member.Name);
					if (correspondingMember is Worker nextWorker)
					{
						Debug($"Found next worker: {nextWorker.Name}");
						rootWorker = nextWorker;
					}
					else if (correspondingMember is ISyncList list)
					{
						Debug($"Found list: {list.Name}");
						var genericArg = list.GetType().GetGenericArguments()[0];
						if (syncElementListType.IsAssignableFrom(list.GetType()))
						{
							foreach (var elem in list.Elements)
							{
								if (elem is ISyncMember listMember && listMember.Name == memberToFind.Name)
								{
									Debug($"Found list member: {listMember.Name}");
									return listMember;
								}
							}
						}
						else if (typeof(Worker).IsAssignableFrom(genericArg))
						{
							Debug("List of workers");
							var listWorkerName = pathToMemberToFind.Pop().Name;
							foreach (var elem in list.Elements)
							{
								var listWorker = (Worker)elem;
								if (listWorker.Name == listWorkerName)
								{
									Debug($"Found list worker: {listWorker.Name}");
									var result = FindCorrespondingMember((Worker)elem, memberToFind, pathToMemberToFind);
									if (result != null)
									{
										return result;
									}
									break;
								}
							}
							break;
						}
					}
					else if (correspondingMember is ISyncBag bag)
					{
						Debug($"Found bag: {bag.Name}");
						var genericArg = bag.GetType().GetGenericArguments()[0];
						if (genericArg == memberToFind.GetType())
						{
							foreach (var elem in bag.Values)
							{
								if (elem is ISyncMember bagMember && bagMember.Name == memberToFind.Name)
								{
									Debug($"Found bag member: {bagMember.Name}");
									return bagMember;
								}
							}
						}
						else if (typeof(Worker).IsAssignableFrom(genericArg))
						{
							Debug("Bag of workers");
							var bagWorkerName = pathToMemberToFind.Pop().Name;
							foreach (var elem in bag.Values)
							{
								var bagWorker = (Worker)elem;
								if (bagWorker.Name == bagWorkerName)
								{
									Debug($"Found bag worker: {bagWorker.Name}");
									var result = FindCorrespondingMember((Worker)elem, memberToFind, pathToMemberToFind);
									if (result != null)
									{
										return result;
									}
									break;
								}
							}
							break;
						}
					}
					else if (correspondingMember != null)
					{
						Debug($"Found: {correspondingMember.Name}");
						return correspondingMember;
					}
					else
					{
						Debug("Could not find the member.");
						break;
					}
				}
				Debug("Failed. Returning null");
				return null;
			}

			bool SetupValueCopy(IField fromField, IField toField, bool undoable = false)
			{
				MethodInfo copyMethod;
				if (toField is ISyncRef syncRef)
				{
					copyMethod = AccessTools.Method(typeof(ComponentMemberReplicator), "GetReferenceCopy").MakeGenericMethod(syncRef.TargetType);
				}
				else
				{
					copyMethod = AccessTools.Method(typeof(ComponentMemberReplicator), "GetValueCopy").MakeGenericMethod(toField.ValueType);
				}

				if (toField.IsDriven)
				{
					if (breakExistingDrives.Value)
					{
						toField.ActiveLink.ReleaseLink(undoable: undoable);
					}
					else
					{
						Debug("Field is driven, skipping");
						return false;
					}
				}
				else if (undoable)
				{
					SafeFieldUndoPoint(toField);
				}

				var valueCopy = (Component)copyMethod.Invoke(null, new object[] { fromField, toField });
				if (undoable)
				{
					valueCopy.CreateSpawnUndoPoint();
				}
				return valueCopy != null;
			}

			void RegenerateWizardUI()
			{
				WizardSearchDataSlot.DestroyChildren();
				WizardStaticContentSlot.DestroyChildren();
				var staticContentRect = WizardStaticContentSlot.GetComponent<RectTransform>();
				WizardUI.ForceNext = staticContentRect;
				WizardStaticContentSlot.RemoveAllComponents((Component c) => c != staticContentRect);

				searchRoot = WizardSearchDataSlot.FindChildOrAdd("searchRoot").GetComponentOrAttach<ReferenceField<Slot>>();
				sourceComponent = WizardSearchDataSlot.FindChildOrAdd("sourceComponent").GetComponentOrAttach<ReferenceField<Component>>();
				targetComponent = WizardSearchDataSlot.FindChildOrAdd("targetComponent").GetComponentOrAttach<ReferenceField<Component>>();

				modeField = WizardSearchDataSlot.FindChildOrAdd("modeField").GetComponentOrAttach<ValueField<int>>();

				breakExistingDrives = WizardSearchDataSlot.FindChildOrAdd("breakExistingDrives").GetComponentOrAttach<ValueField<bool>>();

				VerticalLayout verticalLayout = WizardUI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
				verticalLayout.ForceExpandHeight.Value = false;

				WizardUI.Text("<color=gray>Remember: Source and Target Components must be the same Type!</color>");
				SyncMemberEditorBuilder.Build(sourceComponent.Reference, "Source Component", null, WizardUI);
				SyncMemberEditorBuilder.Build(targetComponent.Reference, "Target Component", null, WizardUI);
				SyncMemberEditorBuilder.Build(searchRoot.Reference, "(or) Target Hierarchy Slot", null, WizardUI);

				WizardUI.Spacer(24f);

				WizardUI.Text("Mode:");
				WizardUI.ValueRadio<int>("Write".AsLocaleKey(), modeField.Value, 0);
				WizardUI.ValueRadio<int>("Drive From Source".AsLocaleKey(), modeField.Value, 1);
				WizardUI.ValueRadio<int>("Copy Existing Drives From Source".AsLocaleKey(), modeField.Value, 2);
				WizardUI.ValueRadio<int>("Write Or Copy Existing Drives From Source".AsLocaleKey(), modeField.Value, 3);

				SyncMemberEditorBuilder.Build(breakExistingDrives.Value, "Break Existing Drives On Target", null, WizardUI);

				WizardUI.Spacer(24f);

				WizardUI.Text("<color=hero.red>WARNING: Effects may not be fully undoable. Proceed with care!</color>");
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

				WizardUI.PopStyle();

				void TargetChanged(IChangeable changeable)
				{
					var syncRef = (ISyncRef)changeable;
					if (syncRef.Target != null)
					{
						ISyncRef other;
						if (syncRef == targetComponent.Reference)
						{
							other = searchRoot.Reference;
						}
						else
						{
							other = targetComponent.Reference;
						}

						other.Changed -= TargetChanged;
						try
						{
							other.Target = null;
						}
						finally
						{
							other.Changed += TargetChanged;
						}
					}
				}


				if (!subscribedToReferenceChanges)
				{
					targetComponent.Reference.Changed += TargetChanged;
					searchRoot.Reference.Changed += TargetChanged;

					sourceComponent.Reference.Changed += (reference) =>
					{
						WizardGeneratedContentSlot.DestroyChildren();
						WizardUI.NestInto(WizardGeneratedContentSlot);
						if (((ISyncRef)reference).Target != null)
						{
							WizardUI.Button("Select All").LocalPressed += (btn, data) =>
							{
								SetEnabledFields(true);
							};
							WizardUI.Button("Deselect All").LocalPressed += (btn, data) =>
							{
								SetEnabledFields(false);
							};

							WizardUI.Spacer(24f);

							WizardUI.PushStyle(); // 1

							WizardUI.Style.MinWidth = -1f;
							WizardUI.Style.MinHeight = -1f;
							WizardUI.Style.PreferredWidth = -1f;
							WizardUI.Style.PreferredHeight = -1f;
							WizardUI.Style.FlexibleWidth = -1f;
							WizardUI.Style.FlexibleHeight = -1f;

							WizardUI.PushStyle(); // 2
							WizardUI.Style.FlexibleHeight = 1f;
							WizardUI.ScrollArea();
							WizardUI.FitContent(SizeFit.Disabled, SizeFit.PreferredSize);
							WizardUI.PopStyle(); // 2

							VerticalLayout fieldsVerticalLayout = WizardUI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
							fieldsVerticalLayout.ForceExpandHeight.Value = false;

							WizardUI.Style.MinWidth = -1f;
							WizardUI.Style.MinHeight = 24f;
							WizardUI.Style.PreferredWidth = -1f;
							WizardUI.Style.PreferredHeight = -1f;
							WizardUI.Style.FlexibleWidth = 1000f;
							WizardUI.Style.FlexibleHeight = -1f;

							workerMemberFields.Clear();

							GenerateWorkerMemberEditors(WizardUI, sourceComponent.Reference.Target);

							WizardUI.PopStyle(); // 1

							WizardUI.NestOut(); // Out of GeneratedFieldsSlot, Into ScrollArea slot
							WizardUI.NestOut(); // Out of ScrollArea slot, Into WizardGeneratedContentSlot

							WizardUI.Spacer(24f);

							var applyButton = WizardUI.Button("Copy Values (Undoable)");
							applyButton.LocalPressed += (btn, data) =>
							{
								Debug("Apply pressed");
								Apply();
							};

							WizardUI.Spacer(24f);
						}
						UpdateCanvasSize();
					};

					subscribedToReferenceChanges = true;
				}
				

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
					//Debug("syncMember Name: " + syncMember.Name);

					if (syncMember is SyncObject)
					{
						Debug("syncMember Name: " + syncMember.Name);
						Debug("Is SyncObject");
						HandleWorker((Worker)syncMember);
					}
					else if (syncMember is IField)
					{
						if (!workerMemberFields[worker.Name].ContainsKey(syncMember.Name))
						{
							Warn("syncMember not in dictionary. Skipping.");
							return;
						}

						SyncMemberData fieldsStruct = workerMemberFields[worker.Name][syncMember.Name];
						if (fieldsStruct.enabledField.Value == false) continue;

						Debug("syncMember Name: " + syncMember.Name);
						Debug("Is IField");

						ISyncMember sourceMember = fieldsStruct.sourceSyncMember;

						// copy drives
						var sourceField = (IField)sourceMember;
						if (DriveFromSource)
						{
							if (SetupValueCopy(sourceField, (IField)syncMember, undoable: true))
							{
								Debug("Setup value copy.");
							}
							else
							{
								Debug("Failed to setup value copy.");
							}
						}
						else if (CopyExistingDrivesFromSource && sourceField.IsDriven)
						{
							if (CopyDrives((SyncElement)sourceField, (SyncElement)syncMember, newCompMappings, undoable: true, recursive: CopyExistingDrivesFromSource))
							{
								Debug("Deep copied drives.");
							}
							else
							{
								Debug("Failed to deep copy drives.");
							}
						}
						else if (ShouldWrite)
						{
							var targetField = (IField)syncMember;

							if (targetField.IsDriven)
							{
								if (breakExistingDrives.Value)
								{
									var link = targetField.ActiveLink;
									link.ReleaseLink(undoable: true);
								}
								else if (!targetField.IsHooked)
								{
									Debug("Field is driven and not hooked, skipping because break drives is not checked");
									continue;
								}
							}
							else
							{
								SafeFieldUndoPoint(targetField);
							}

							syncMember.CopyValues(sourceMember);

							Debug("Values written.");
						}
						else
						{
							Debug("Nothing to do.");
						}
					}
					else if (syncMember is SyncElement)
					{
						if (!workerMemberFields[worker.Name].ContainsKey(syncMember.Name))
						{
							Warn("syncMember not in dictionary. Skipping.");
							continue;
						}

						SyncMemberData fieldsStruct = workerMemberFields[worker.Name][syncMember.Name];
						if (fieldsStruct.enabledField.Value == false) continue;

						Debug("syncMember Name: " + syncMember.Name);
						Debug("Is SyncElement");

						ISyncMember sourceMember = fieldsStruct.sourceSyncMember;

						if (ShouldWrite)
						{
							if (syncMember.IsDriven)
							{
								if (breakExistingDrives.Value)
								{
									syncMember.ActiveLink.ReleaseLink(undoable: true);
								}
								else if (!syncMember.IsHooked)
								{
									Debug("SyncElement is driven and not hooked, skipping because break drives is not checked");
									continue;
								}
							}
							syncMember.CopyValues(sourceMember);
							Debug("Values written.");
						}

						if (ShouldDrive)
						{
							// Restore drives to list and bag elements
							if (sourceMember is ISyncList sourceList)
							{
								Debug("Is List");

								if (sourceList.IsDriven && CopyExistingDrivesFromSource)
								{
									Debug($"Driven list to restore: {ElementIdentifierString(sourceList)}");
									if (CopyDrives((SyncElement)sourceMember, (SyncElement)syncMember, newCompMappings, undoable: true, recursive: true))
									{
										Debug("Deep copied drives.");
									}
									else
									{
										Debug("Failed to deep copy drives.");
									}
									continue;
								}

								var allDriveData = new List<DriveData>();
								foreach (var elem in sourceList.Elements)
								{
									if (elem is IField listField)
									{
										if (DriveFromSource || (CopyExistingDrivesFromSource && listField.IsDriven))
										{
											var driveData = new DriveData();
											driveData.targetMember = listField;
											driveData.stackToTargetMember = GetMemberStack(listField);
											allDriveData.Add(driveData);
										}
									}
									else if (elem is Worker listWorker)
									{
										CollectDriveData(listWorker, allDriveData);
									}
								}
								foreach (var driveData in allDriveData)
								{
									Debug($"Drive data member: {ElementIdentifierString(driveData.targetMember)}");
									var correspondingMember = FindCorrespondingMember(syncMember.FindNearestParent<Component>(), driveData.targetMember, driveData.stackToTargetMember);
									if (DriveFromSource)
									{
										if (SetupValueCopy((IField)driveData.targetMember, (IField)correspondingMember, undoable: true))
										{
											Debug("Setup value copy.");
										}
										else
										{
											Debug("Failed to setup value copy.");
										}
									}
									else if (CopyExistingDrivesFromSource)
									{
										if (CopyDrives((SyncElement)driveData.targetMember, (SyncElement)correspondingMember, newCompMappings, undoable: true, recursive: true))
										{
											Debug("Deep copied drives.");
										}
										else
										{
											Debug("Failed to deep copy drives.");
										}
									}
								}
							}
							else if (sourceMember is ISyncBag sourceBag)
							{
								Debug("Is Bag");

								if (sourceBag.IsDriven && CopyExistingDrivesFromSource)
								{
									Debug($"Driven bag to restore: {ElementIdentifierString(sourceBag)}");
									if (CopyDrives((SyncElement)sourceMember, (SyncElement)syncMember, newCompMappings, undoable: true, recursive: true))
									{
										Debug("Deep copied drives.");
									}
									else
									{
										Debug("Failed to deep copy drives.");
									}
									continue;
								}

								var allDriveData = new List<DriveData>();
								foreach (var elem in sourceBag.Values)
								{
									if (elem is IField bagField)
									{
										if (DriveFromSource || (CopyExistingDrivesFromSource && bagField.IsDriven))
										{
											var driveData = new DriveData();
											driveData.targetMember = bagField;
											driveData.stackToTargetMember = GetMemberStack(bagField);
											allDriveData.Add(driveData);
										}
									}
									else if (elem is Worker bagWorker)
									{
										CollectDriveData(bagWorker, allDriveData);
									}
								}
								foreach (var driveData in allDriveData)
								{
									Debug($"Drive data member: {ElementIdentifierString(driveData.targetMember)}");
									var correspondingMember = FindCorrespondingMember(syncMember.FindNearestParent<Component>(), driveData.targetMember, driveData.stackToTargetMember);
									if (DriveFromSource)
									{
										if (SetupValueCopy((IField)driveData.targetMember, (IField)correspondingMember, undoable: true))
										{
											Debug("Setup value copy.");
										}
										else
										{
											Debug("Failed to setup value copy.");
										}
									}
									else if (CopyExistingDrivesFromSource)
									{
										if (CopyDrives((SyncElement)driveData.targetMember, (SyncElement)correspondingMember, newCompMappings, undoable: true, recursive: true))
										{
											Debug("Deep copied drives.");
										}
										else
										{
											Debug("Failed to deep copy drives.");
										}
									}
								}
							}
							else if (sourceMember is SyncPlayback sourcePlayback)
							{
								if (DriveFromSource)
								{
									if (syncMember.IsDriven)
									{
										if (breakExistingDrives.Value)
										{
											syncMember.ActiveLink.ReleaseLink(undoable: true);
										}
										else
										{
											Debug("Playback is driven and break drives is not checked, skipping...");
											continue;
										}
									}
									var playbackSynchronizer = syncMember.FindNearestParent<Slot>().AttachComponent<PlaybackSynchronizer>();
									var targetPlayback = (SyncPlayback)syncMember;
									playbackSynchronizer.Source.Target = sourcePlayback;
									playbackSynchronizer.Target.Target = targetPlayback;
									playbackSynchronizer.CreateSpawnUndoPoint();
									Debug("Synchronized playbacks.");
								}
								else if (CopyExistingDrivesFromSource)
								{
									if (sourcePlayback.IsDriven)
									{
										Debug($"Driven playback to restore: {ElementIdentifierString(sourcePlayback)}");
										//var correspondingMember = FindCorrespondingMember(syncMember.FindNearestParent<Component>(), sourcePlayback, GetMemberStack(sourcePlayback));
										if (CopyDrives((SyncElement)sourceMember, (SyncElement)syncMember, newCompMappings, undoable: true, recursive: true))
										{
											Debug("Deep copied drives.");
										}
										else
										{
											Debug("Failed to deep copy drives.");
										}
									}
								}
							}

							// is it possible to DriveFromSource a SyncArray? could maybe get drive data for the array... need to get references to the elements and make sure they are IFields or ISyncRefs compatible with ValueCopy/RefCopy
							// would be possible but weird? idk, not sure they are used much anywhere
							//else if (sourceMember is ISyncArray sourceArray)
							//{

							//}

							// technically could handle SyncDictionaries but i don't think they are used much anywhere atm, although SyncFieldDictionary is a thing
							//else if (sourceMember is ISyncDictionary sourceDict)
							//{

							//}

							else
							{
								// Handle arbitrary SyncElement
								if (CopyExistingDrivesFromSource)
								{
									if (sourceMember.IsDriven)
									{
										Debug($"Driven SyncElement to restore: {ElementIdentifierString(sourceMember)}");
										//var correspondingMember = FindCorrespondingMember(syncMember.FindNearestParent<Component>(), sourceMember, GetMemberStack(sourceMember));
										if (CopyDrives((SyncElement)sourceMember, (SyncElement)syncMember, newCompMappings, undoable: true, recursive: true))
										{
											Debug("Deep copied drives.");
										}
										else
										{
											Debug("Failed to deep copy drives.");
										}
									}
								}
							}
						}
					}
					else
					{
						Debug("syncMember is not supported type: " + syncMember.GetType().Name ?? "NULL");
					}
				}
			}

			private void Apply()
			{
				if (sourceComponent.Reference.Target == null)
				{
					Debug("sourceComponent is null!");
					return;
				}
				if (searchRoot.Reference.Target == null && targetComponent.Reference.Target == null)
				{
					Debug("searchRoot and targetComponent are null!");
					return;
				}

				if (sourceComponent.Reference.Target == targetComponent.Reference.Target)
				{
					Debug("Source component is the same reference as target component!");
					return;
				}

				if (targetComponent.Reference.Target != null && targetComponent.Reference.Target.GetType() != sourceComponent.Reference.Target.GetType())
				{
					Debug("Target component is not the same type as source component!");
					return;
				}

				var createdUndoBatch = false;

				List<Component> componentsList;
				if (targetComponent.Reference.Target != null)
				{
					componentsList = new List<Component>() { targetComponent.Reference.Target };
				}
				else
				{
					componentsList = searchRoot.Reference.Target.GetComponentsInChildren((Component c) =>
					c.GetType() == sourceComponent.Reference.Target.GetType() && c != sourceComponent.Reference.Target);
				}

				if (componentsList.Count > 0 && workerMemberFields.Values.Any(dict => dict.Values.Any(syncMemberData => syncMemberData.sourceSyncMember is IField)))
				{
					WizardSlot.World.BeginUndoBatch("Set component members");
					createdUndoBatch = true;
				}

				foreach (Component c in componentsList)
				{
					newCompMappings.Clear();
					Debug(c.Name);
					HandleWorker(c);
				}

				if (createdUndoBatch)
				{
					WizardSlot.World.EndUndoBatch();
				}
			}

			void GenerateWorkerMemberEditors(UIBuilder UI, Worker targetWorker, bool recursive = true)
			{
				workerMemberFields.Add(targetWorker.Name, new Dictionary<string, SyncMemberData>());

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
					UI.Style.TextColor = MathX.LerpUnclamped(RadiantUI_Constants.TEXT_COLOR, c, 0.1f); // copying the way field names get colored in the inspector

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
					else if (!(syncMember is SyncElement))
					{
						UI.PushStyle();
						UI.Style.PreferredHeight = 24f;
						UI.Style.TextColor = colorX.Gray;
						UI.Text($"{syncMember.Name} (not supported)").HorizontalAlign.Value = TextHorizontalAlignment.Left;
						UI.PopStyle();
						continue;
					}

					FieldInfo fieldInfo = targetWorker.GetSyncMemberFieldInfo(syncMember.Name);

					var horizontalLayout = UI.HorizontalLayout(4f, childAlignment: Alignment.MiddleLeft);
					horizontalLayout.ForceExpandWidth.Value = false;

					UI.PushStyle();

					UI.Style.MinWidth = 24f;
					UI.Style.MinHeight = 24f;
					UI.Style.PreferredWidth = -1f;
					UI.Style.PreferredHeight = -1f;
					UI.Style.FlexibleWidth = 1f;
					UI.Style.FlexibleHeight = -1f;

					UI.Style.TextColor = RadiantUI_Constants.Neutrals.LIGHT;

					var checkbox = UI.Checkbox(false);

					UI.PopStyle();

					//SyncMemberEditorBuilder.Build(syncMember, syncMember.Name, fieldInfo, UI);

					UI.PushStyle();
					UI.Style.PreferredHeight = 24f;
					if (!(syncMember is IField))// && syncMember is SyncElement)
					{
						//UI.Text($"{syncMember.GetType().GetGenericTypeDefinition().Name ?? syncMember.GetType().GetNiceName()} {syncMember.Name} (not undoable)").HorizontalAlign.Value = TextHorizontalAlignment.Left;
						UI.Text($"{syncMember.Name} <color=hero.red>(not undoable)</color>").HorizontalAlign.Value = TextHorizontalAlignment.Left;
					}
					else
					{
						UI.Text($"{syncMember.Name}").HorizontalAlign.Value = TextHorizontalAlignment.Left;
					}
					UI.PopStyle();

					//UI.MemberEditor((IField)syncMember, )

					SyncMemberData fieldsStruct = new SyncMemberData();
					fieldsStruct.sourceSyncMember = syncMember;
					fieldsStruct.enabledField = checkbox.State;

					workerMemberFields[targetWorker.Name].Add(syncMember.Name, fieldsStruct);

					UI.NestOut();
				}
			}
		}
	}
}