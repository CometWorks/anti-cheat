# Graph Report - .  (2026-07-12)

## Corpus Check
- Corpus is ~18,699 words - fits in a single context window. You may not need a graph.

## Summary
- 569 nodes · 933 edges · 42 communities (34 shown, 8 thin omitted)
- Extraction: 99% EXTRACTED · 1% INFERRED · 0% AMBIGUOUS · INFERRED: 11 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Network Payload Guards
- Config Storage UI
- Shared Configuration
- Client Config UI
- Admin Audit
- Harmony IL Matching
- Preloader IL Tools
- Ship Tool Guards
- Log Formatting
- Client Plugin Lifecycle
- Plugin Initialization
- Terminal Value Bounds
- Build Projects
- Server Plugin Architecture
- Setup Script
- Toolbar Safety
- Keybind Controls
- Slider Controls
- Toolbar Request Guards
- Dropdown Controls
- Validation Enforcement
- Persistent Configuration
- Button Controls
- Color Controls
- Settings Layout
- Checkbox Controls
- Separator Controls
- Textbox Controls
- UI Utility Methods
- Settings Elements
- Base GUI Control
- Plugin Interfaces
- Plugin Project Topology
- SE Development Guidance
- Clean Script
- Client Deploy Script
- Patch Compatibility
- Server Deploy Script
- Props Verification
- Config Dialog Image
- Template Documentation

## God Nodes (most connected - your core abstractions)
1. `TranspilerHelpers` - 17 edges
2. `PreloaderHelpers` - 16 edges
3. `ClientPlugin.Settings.Elements` - 15 edges
4. `Config` - 14 edges
5. `Plugin` - 14 edges
6. `Control` - 14 edges
7. `IPluginLogger` - 14 edges
8. `IElement` - 13 edges
9. `SettingsGenerator` - 13 edges
10. `TerminalPropertyBoundsPatch` - 13 edges

## Surprising Connections (you probably didn't know these)
- `Plugin` --references--> `IPluginConfig`  [EXTRACTED]
  ClientPlugin/Plugin.cs → Shared/Config/IPluginConfig.cs
- `Plugin` --references--> `PersistentConfig`  [EXTRACTED]
  ClientPlugin/Plugin.cs → Shared/Config/PersistentConfig.cs
- `Plugin` --references--> `IPluginLogger`  [EXTRACTED]
  ClientPlugin/Plugin.cs → Shared/Logging/IPluginLogger.cs
- `Plugin` --implements--> `ICommonPlugin`  [EXTRACTED]
  ClientPlugin/Plugin.cs → Shared/Plugin/ICommonPlugin.cs
- `Plugin` --references--> `IPluginConfig`  [EXTRACTED]
  ServerPlugin/Plugin.cs → Shared/Config/IPluginConfig.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Client, Server, and Shared Plugin Architecture** — readme_client_plugin_project, readme_server_plugin_project, readme_shared_project [EXTRACTED 1.00]

## Communities (42 total, 8 thin omitted)

### Community 0 - "Network Payload Guards"
Cohesion: 0.06
Nodes (20): BlockList, double, float, HashSet, int, List, MethodBase, Vector2 (+12 more)

### Community 1 - "Config Storage UI"
Cohesion: 0.06
Nodes (27): ConfigStorage, string, Layout, Func, List, MyGuiControlBase, Vector2, None (+19 more)

### Community 2 - "Shared Configuration"
Cohesion: 0.06
Nodes (24): Assembly, System.Runtime.CompilerServices, Shared.Tools, Exception, bool, int, PluginConfig, ConstructorInfo (+16 more)

### Community 3 - "Client Config UI"
Cohesion: 0.08
Nodes (20): Button, Config, ExampleEnum, bool, Color, float, int, string (+12 more)

### Community 4 - "Admin Audit"
Cohesion: 0.10
Nodes (17): MyCubeBlockDefinition, MyCubeGrid, MyOwnershipShareModeEnum, MyPasteGridParameters, Dictionary, MethodBase, MyCubeBlock, MySlimBlock (+9 more)

### Community 5 - "Harmony IL Matching"
Cohesion: 0.18
Nodes (11): FieldInfo, FieldInfoPredicate, Label, OpcodePredicate, CodeInstruction, CodeInstructionPredicate, IEnumerable, List (+3 more)

### Community 6 - "Preloader IL Tools"
Cohesion: 0.21
Nodes (9): Collection, FieldReference, MethodDefinition, MethodReference, CodeInstructionPredicate, Instruction, List, PreloaderHelpers (+1 more)

### Community 7 - "Ship Tool Guards"
Cohesion: 0.14
Nodes (13): MyDrillBase, MyEntity, MyShipGrinder, MyShipWelder, Dictionary, HashSet, MyCubeBlock, MySlimBlock (+5 more)

### Community 8 - "Log Formatting"
Cohesion: 0.22
Nodes (10): Exception, int, MethodImpl, string, LogFormatter, Exception, MethodImpl, PluginLogger (+2 more)

### Community 9 - "Client Plugin Lifecycle"
Cohesion: 0.16
Nodes (7): Plugin, bool, string, IPlugin, bool, string, Plugin

### Community 10 - "Plugin Initialization"
Cohesion: 0.23
Nodes (6): MethodImpl, Harmony, MethodImpl, Exception, IPluginLogger, PatchHelpers

### Community 11 - "Terminal Value Bounds"
Cohesion: 0.28
Nodes (5): MyPropertySyncStateGroup, PropertyInfo, float, MyCubeBlock, TerminalPropertyBoundsPatch

### Community 12 - "Build Projects"
Cohesion: 0.13
Nodes (11): net10.0, net48, Lib.Harmony (2.4.2), Mono.Cecil (0.11.6), Microsoft.NET.Sdk, Shared, net10.0, net48 (+3 more)

### Community 13 - "Server Plugin Architecture"
Cohesion: 0.22
Nodes (6): Shared.Plugin, Shared.Config, Shared.Logging, ServerPlugin, Shared.Patches, ChangeOwnerToolbarCleanupPatch

### Community 14 - "Setup Script"
Cohesion: 0.25
Nodes (14): _ensure_props(), _generate_guid(), _get_install_locations(), _get_linux_steam_path(), _get_steam_path(), _get_windows_steam_path(), _input_plugin_name(), _input_question() (+6 more)

### Community 15 - "Toolbar Safety"
Cohesion: 0.36
Nodes (5): MyToolbar, MyToolbarItem, bool, MyCubeBlock, ToolbarSetItemGuardPatch

### Community 16 - "Keybind Controls"
Cohesion: 0.31
Nodes (7): KeybindAttribute, Action, Func, List, string, Type, MyGuiControlButton

### Community 17 - "Slider Controls"
Cohesion: 0.20
Nodes (9): SliderAttribute, SliderType, Action, float, Func, List, string, Type (+1 more)

### Community 18 - "Toolbar Request Guards"
Cohesion: 0.38
Nodes (5): HarmonyPatch, HarmonyPrefix, MyObjectBuilder_ToolbarItem, int, ToolbarRequestPatch

### Community 19 - "Dropdown Controls"
Cohesion: 0.24
Nodes (7): DropdownAttribute, Action, Func, int, List, string, Type

### Community 20 - "Validation Enforcement"
Cohesion: 0.20
Nodes (8): DateTime, HarmonyPostfix, Dictionary, HashSet, int, List, FailureWindow, ValidationEnforcementPatch

### Community 21 - "Persistent Configuration"
Cohesion: 0.29
Nodes (5): IDisposable, PropertyChangedEventArgs, int, PersistentConfig, Timer

### Community 22 - "Button Controls"
Cohesion: 0.25
Nodes (7): Attribute, ButtonAttribute, Action, Func, List, string, Type

### Community 23 - "Color Controls"
Cohesion: 0.25
Nodes (8): ColorAttribute, Action, bool, Color, Func, List, string, Type

### Community 24 - "Settings Layout"
Cohesion: 0.22
Nodes (7): Simple, float, List, MyGuiControlBase, Vector2, MyGuiControlParent, MyGuiControlScrollablePanel

### Community 25 - "Checkbox Controls"
Cohesion: 0.29
Nodes (6): CheckboxAttribute, Action, Func, List, string, Type

### Community 26 - "Separator Controls"
Cohesion: 0.29
Nodes (6): SeparatorAttribute, Action, Func, List, string, Type

### Community 27 - "Textbox Controls"
Cohesion: 0.29
Nodes (6): TextboxAttribute, Action, Func, List, string, Type

### Community 28 - "UI Utility Methods"
Cohesion: 0.36
Nodes (3): Tools, Color, Regex

### Community 29 - "Settings Elements"
Cohesion: 0.33
Nodes (5): IElement, Action, Func, List, Type

### Community 30 - "Base GUI Control"
Cohesion: 0.33
Nodes (5): Control, float, MyGuiControlBase, Vector2, MyGuiDrawAlignEnum

### Community 31 - "Plugin Interfaces"
Cohesion: 0.40
Nodes (5): INotifyPropertyChanged, IPluginConfig, string, Common, ICommonPlugin

### Community 32 - "Plugin Project Topology"
Cohesion: 0.67
Nodes (3): ClientPlugin Project, ServerPlugin Project, Shared Project

## Knowledge Gaps
- **25 isolated node(s):** `Shared`, `net10.0`, `net48`, `Lib.Harmony (2.4.2)`, `Mono.Cecil (0.11.6)` (+20 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **8 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Shared.Plugin` connect `Server Plugin Architecture` to `Network Payload Guards`, `Client Config UI`, `Admin Audit`, `Ship Tool Guards`, `Validation Enforcement`, `Plugin Interfaces`?**
  _High betweenness centrality (0.213) - this node is a cross-community bridge._
- **Why does `Shared.Patches` connect `Server Plugin Architecture` to `Network Payload Guards`, `Client Config UI`, `Admin Audit`, `Ship Tool Guards`, `Validation Enforcement`?**
  _High betweenness centrality (0.205) - this node is a cross-community bridge._
- **Why does `ClientPlugin.Settings.Elements` connect `Client Config UI` to `Slider Controls`, `Dropdown Controls`, `Button Controls`, `Checkbox Controls`, `Separator Controls`, `Textbox Controls`, `Settings Elements`, `Base GUI Control`?**
  _High betweenness centrality (0.173) - this node is a cross-community bridge._
- **What connects `Shared`, `net10.0`, `net48` to the rest of the system?**
  _26 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Network Payload Guards` be split into smaller, more focused modules?**
  _Cohesion score 0.05782312925170068 - nodes in this community are weakly interconnected._
- **Should `Config Storage UI` be split into smaller, more focused modules?**
  _Cohesion score 0.05603864734299517 - nodes in this community are weakly interconnected._
- **Should `Shared Configuration` be split into smaller, more focused modules?**
  _Cohesion score 0.06090808416389812 - nodes in this community are weakly interconnected._