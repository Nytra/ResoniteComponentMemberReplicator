# ComponentMemberReplicator

![2024-10-14 03 35 05](https://github.com/user-attachments/assets/25abf816-ffcc-4da0-bb22-e70851527e01)

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/) that adds a wizard to allow copying selected values from a source component and applying them to one or many other components of the same Type.

Can also (re)create drives. Values can be driven directly from the source component, or existing drive chains can be copied over to the targets.

This is a powerful builder tool and care should be taken when using it. While most of the effects of the mod are undoable, some cannot be. It would be a good practice to make a backup of your world or item before using this.

The wizard can be found in the Developer Tool's 'Create New' menu under Editors/Component Member Replicator (Mod).

## Modes

- Write
  - Writes the selected values from the source component to the target components. Sometimes the effects of this may not be undoable if the target member is being hooked (i.e. something else is listening to its changes), it can receives further changes from an external source after the initial write happens which will not be undoable.

- Drive From Source
  - Creates a ValueCopy or ReferenceCopy to drive the target fields. If targetting a playback, PlaybackSynchronizer will be created. If targetting a list or bag, the source list or bag will be written to the target to ensure the correct number of elements, and then all fields/playbacks will be driven.

- Copy Existing Drives From Source
  - If a selected member on the source component is driven, it copies the driving components recursively over to the target component's slot and drives the target member. This will copy multiple components if they are driven in a chain.
 
- Write And Copy Existing Drives From Source
  - Same as above, but if a selected member on the source component is not driven it will perform a write instead.
 
## Options

- Break Existing Drives On Target
  - If the selected member on the target component is driven, it will break the drive. This is an undoable operation.

## Installation
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
2. Place [ComponentMemberReplicator.dll](https://github.com/Nytra/ResoniteComponentMemberReplicator/releases/latest/download/ComponentMemberReplicator.dll) into your `rml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a default install. You can create it if it's missing, or if you launch the game once with ResoniteModLoader installed it will create the folder for you.
3. Start the game. If you want to verify that the mod is working you can check your Resonite logs.
