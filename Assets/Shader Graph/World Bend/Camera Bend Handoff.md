# Material-based road bend handoff

## Scene and controls

The active and saved scene is `Assets/Scenes/M7MD_Albitar.unity` in Unity 6000.3.24f1. Select Main Camera and find **Jordan Material Road Bend**. Use the **Bend** slider for left (negative), right (positive) or straight (zero). The saved value +0.085 uses 85% of the configured **Maximum Angle** of 8° and approaches 6.8° in the distance. **Bend Start** is a signed distance in metres relative to the runner. Negative values start the curve behind the runner. **Bend Entry Distance** accepts 4 m or more and measures the distance from Bend Start to half the effective final angle. **Bend Exponent** ranges from 2 to 5. Higher values keep the initial turn smaller and build heading faster farther along the entry. **Entry Softness** broadens the heading transition. Short entries with steep settings can reduce the effective final angle to keep the inside curb from folding. The controller checks a 12 m half-width around the road center. The bend keeps developing beyond the buildup distance as it approaches the maximum. The camera uses its native projection.

The scene viewport displays the undeformed assets in Edit Mode. Inspect the bend from the gameplay camera in Play Mode.

## Assets and assignments

- `JordanRoadBendLit.shadergraph` is a shared URP Lit graph. Separate Custom Function nodes connect object-space Position, Normal and Tangent to the Vertex stack using the same curve math. The position path depends only on Position, which lets URP compile the MotionVectors pass. The fragment stack uses base map, tint, metallic, smoothness and emission. Texture scale and offset are included.
- `RoadCurve.hlsl` bends geometry on a shared world-space centerline with cross-sections perpendicular to the local direction. Farther ahead the heading approaches a fixed yaw as the change in heading tapers off. The geometry under the player uses the original object-space vertex data.
- `Road Bend Materials/` contains 26 named material assets. There are 25 distinct source appearances across 413 MeshRenderers on the six course roots. Each source maps to one replacement, and repeated objects with the same source share it. The roads use `M_Bend_black`, lane markings and curbs keep corresponding scene color materials, and bus body, glass, windows, lights, wheels and other slots preserve their source ordering. `M_Bend_gold` is used by the runtime coin prefab. The three base textures used by the bus lights, turn signals and number plate retain their texture bindings.
- `Road Bend Meshes/` contains four subdivided visual meshes assigned to 30 long street and sidewalk instances. This increases the geometry sampling through the bend. The existing inactive Ground planes keep their original mesh.
- `Assets/Scripts/Shader gragh/JordanMaterialRoadBend.cs` supplies the world-space curve during the active gameplay camera render and adjusts its CPU culling at nonzero strength. It binds the configured gold material to production coin pool instances. The road, lane markings, curbs, buses and scenery use serialized scene-instance assignments. The older camera bend code and material were absent when work began. The existing fullscreen feature references resolved to no material in the tested pipelines.
- The scene stores the Main Camera component, material overrides and visual mesh overrides. Production geometry assets and collisions retain their original assignment.

## Prior material conversion checks

Tests used Mobile quality in the Windows Unity editor. Rendered identical portrait camera poses at strengths zero and both signs. A normal lane change took the player from X=0 to X=5. Captured the ascent at Y≈0.66 m and the flat DualBusses roof at Y≈3.40 m, confirmed by a physics ray. The camera projection, transform and lens stayed identical across each pair. Both signs bent the distant lane markings and obstacle group in the corresponding direction. The lower half of the tested ramp and roof frames matched the straight reference pixel-for-pixel. At those positions all 427 currently active MeshRenderers used the graph. At the earlier ground pose all 460 were graph-bound after rendering, including all 140 active pooled coins.

The recycled replacement root, Enviroment, had 115 active renderers and 211/211 graph material slots. All 160 active coins and 486 active scene MeshRenderers used the graph after replacement. In the fixed portrait overview before and after turnover, lane markings and curbs stayed visually continuous. The runner was on the roof before the threshold. A later obstacle collision stopped the run, so the final turnover was verified by resuming the existing production chunk scrolling separately.

After splitting the vertex outputs, Shader Graph imported without errors and compiled the Android default-keyword Universal Forward, ShadowCaster, MotionVectors, DepthOnly and DepthNormals programs on both Vulkan and OpenGL ES 3 (10 of 10). A final Mobile Play Mode ground rerun changed approximately 12,100 of 103,040 pixels with either signed setting relative to zero. About 50 lower-half pixels differed, and the camera projection was identical. All 140 active coins used the graph. The Unity Console reported zero errors after exit. Physical Android performance and appearance remain to be tested.

Git scope inspection showed the new scene overrides only in the requested scene. The local implementation is uncommitted and based on e75a9cb.

## Prior polynomial bend checks

On 24 November 2026 the existing camera script and shared HLSL were updated. The camera sends the signed target slope and the buildup power. The shader integrates an increasing heading blend and uses its analytic derivative for the lane frame, in the same world space as before. The runner and first 12 m have zero deformation. At the default settings, the road center has moved about 4.5 mm at 20 m, 24 cm at 35 m, and 2.18 m at 58 m. Its final heading stays constant beyond 58 m. Existing materials, mesh overrides, and coin binding continue to drive the render.

- In the Windows Unity Editor on the Mobile profile, rendered the same 224 × 460 camera pose at Bend 0, +0.085 and −0.085 on the ground, on the ramp and at the bus roof. Far lane edges bent in the chosen direction with the same projection throughout. The bottom third of the ramp and roof captures matched the zero frame pixel-for-pixel. In the ground photo the bottom third differed by 42 and 25 pixels for the signed previews.
- During ordinary gameplay the runner changed lanes through x=0 → 5 → 0 → −5, went up the ramp at y≈0.72 m and reached the roof at y≈3.31 m. A downward ray hit the flat SingleBus collider. The live run continued through chunk retirement and replacement. The replacement used the graph in 211/211 populated material slots, and all 116 active coin renderers used it.
- The generated shader passed default-keyword Android compilation for Universal Forward, ShadowCaster, MotionVectors, DepthOnly and DepthNormals on Vulkan and OpenGL ES 3. Real-device appearance and performance remain untested.
- Restored Edit Mode and selected Main Camera. The scene already had unsaved changes before these tests. Its on-disk hash and dirty state matched the starting state at the end. This update touched the camera script, shared HLSL and this handoff. It remains uncommitted.

## Exponential bend update

The distance formula now uses a squared logistic profile driven by an exponential, with an analytic integral and derivative for the centerline and lane frame. The blend starts at zero with zero slope, grows through the visible turn, and approaches the target heading smoothly. The previous polynomial fixed its heading 58 m ahead of the runner. The new default heading is approximately 1.53° at 58 m, 3.72° at 75 m, 5.96° at 100 m, and 6.62° at 125 m. The limits of the slider and existing serialized values were retained.

Retested in Unity Play Mode on the Mobile quality profile. At the same ground camera pose, 224 × 460 captures at 0, +0.085, and −0.085 showed a straight foreground with opposite distant turns and identical projection. All ten default-keyword Android vertex variant checks passed across Vulkan and OpenGL ES 3. The earlier ramp, roof and chunk checks above are from the polynomial revision. This exponential revision was verified on the ground in the Editor. Device appearance and performance remain open.

GPT-6.2 Astra (high effort) implemented the exponential shader, and GPT-6.2 Sol agents (medium effort) checked the profile independently and updated the Inspector labels. The primary GPT-6.2 Sol session ran live verification.

## Reproduce

1. Open `M7MD_Albitar` and select Mobile portrait in the Game or Simulator view. Enter Play Mode with the Main Camera component enabled.
2. Pause on a ground frame and compare the Inspector strength at 0, +0.085 and −0.085, using the same camera pose. The curve should be readable from the lane markings and curbs ahead. Lower the strength if desired while keeping the same protected distance.
3. Resume with the default positive strength and take an open ramp. Pause once on the incline and once on the roof. Check the ramp, roof edges and pickups around the player, then follow the lane into the turn. Repeat across a chunk recycle to verify persisted assignments and pooled coin coverage.

## Model use

GPT‑6.2 Sol (low) handled the inventory and visual mesh investigation. GPT‑6.2 Astra (high) implemented the Shader Graph and vertex deformation. The primary GPT‑6.2 Sol session handled the scene migration and integration. GPT‑6.2 Sol (medium) ran the original Unity checks.

For the gradual bend update, a GPT‑6.2 Astra agent at high effort reviewed and implemented the shader math. GPT‑6.2 Sol agents at medium effort updated the controller and prepared the verification approach. The primary GPT‑6.2 Sol session ran the live Unity tests.


## Independent vertical bend

Implemented on `main` at base `e75a9cba3ebe34e5f2132e269e2a7d22cbbf0c01`. Verified the open Windows scene `M7MD_Albitar` and captured the prior X-only path on Mobile portrait before editing. The live horizontal values are Bend +0.085, Start 12 m, Entry 46 m, Softness 0.6, Exponent 3, Maximum Angle 8°.

The **Vertical Bend (Y)** section is on Main Camera → **Jordan Material Road Bend**. These fields have separate serialized backing values from X:

| Field | Default | Meaning |
| --- | --- | --- |
| Vertical Bend | 0 | Strength from 0 to 1. Increasing it bends toward negative world Y. Zero gives the existing X-only result. |
| Vertical Bend Start | +16 m | Signed start relative to runner Z. Negative begins behind the runner. |
| Vertical Bend Entry Distance | 32 m | Meters after the start to half the effective pitch for Y alone. Minimum 4 m. |
| Vertical Bend Exponent | 3 | Range 2–5. Larger values soften the first part of the descent around the same midpoint. |
| Maximum Downward Angle | 8° | Target terminal downward angle at full strength. Range 0–15°. |

**Tested starting preset:** Set Vertical Bend to **0.55**, Start **16 m**, Entry **32 m**, Exponent **3**, and Maximum **8°**. This approaches 4.4° downward with Y alone. Predicted centerline drop is 0.31 m at 48 m (2.2° pitch), 2.38 m at 80 m and 3.91 m at 100 m from the runner. Leave the existing X settings in place to combine both. Measuring exact pitch with both active can give a slightly different value because X changes longitudinal travel. Put strength back at zero when finished previewing. The Windows scene is still saved with the default Y strength of zero.

- `RoadCurve.hlsl` adds a shared exponential Y offset evaluated at the same original world Z as X. Points at the same X/Z preserve their world-height offsets. Normals use the inverse transpose and tangents use the derivative of the combined mapping. The existing Position, Normal and Tangent Shader Graph Custom Function entry points continue to drive vertex positions and shading, with no graph asset, material or GUID change. The position-only call remains independent of normal and tangent inputs.
- `Assets/Scripts/Shader gragh/JordanMaterialRoadBend.cs` adds the five Inspector controls and a camera-scoped vertical global, preserving the serialized X controls and the X-only culling branch. The Y and combined paths expand CPU culling for deformed geometry using a bound on the original vertex displacement. The camera projection is unchanged, and the shader globals and culling matrix are restored after rendering. Collider and pool code remain as before.
- `Camera Bend Handoff.md` contains the preset and checks. This task used the existing Windows working tree, with no Git commit. The verified scene was clean before and after, with the same on-disk SHA-256.

### Verification and limits

- Captured matched 224 × 460 Mobile portrait frames in the Windows Editor with both effects off, X only, Y only and X+Y. Both-off retained native culling, active modes widened CPU culling, and all cases restored the original culling matrix and globals. All used the native image projection. The X-only path uses the same horizontal profile and input values as the pre-edit capture. Chunk layouts differed across sessions, so the before/after X-only check was visual and source-based.
- With the same camera pose, vertical exponents 2 and 5 changed the entry profile. Entry 4 m was checked at Start +16 m and at Start −8 m. The negative-start stress setting (strength 0.85, max 12°, exponent 5) visibly lowered the road underneath the player by a predicted 0.70 m while physics remained at its original height. Keep the start ahead of the player for collision-critical routes, especially for short and steep entries.
- Used normal lane input and the gentle preset to reach the ramp at y≈0.73 m and the roof at y≈3.47 m. A downward ray hit the flat DualBusses collider. Compared X-only and combined at both poses and saw the descent in the distance while the nearby ramp and deck aligned with the player. In a second run with the final culling build, a chunk retired and the replacement loaded at t≈5.51 s while gameplay remained active. The new chunk had 211/211 populated Shader Graph material slots and all 116 active coin renderers used the graph. The post-recycle frame showed continuous near curbs and markings, with visible distant descent.
- The culling calculation was also swept over 42,930 numerical lane samples with no missed samples, covering multiple signed starts, terminal angles and frustum depths. The final live 4-mode and recycle renders used the revised bound.
- Android default-keyword vertex variants compiled for Universal Forward, ShadowCaster, DepthOnly, DepthNormals and MotionVectors on both Vulkan and OpenGL ES 3. Unity Console returned zero error entries after testing. Physical Android image quality and performance remain unmeasured.

Inventory used GPT-6.2 Sol at low effort. Shader math and implementation used GPT-6.2 Astra at high effort. Controller and culling implementation plus the independent verification calculations used GPT-6.2 Sol at medium effort. The primary GPT-6.2 Sol session ran the live Unity checks and updated this handoff.


## Saved left turn and descent preset

Saved and reopened on `main` in `Assets/Scenes/M7MD_Albitar.unity`. Only Main Camera's serialized **Jordan Material Road Bend** block was edited in the scene. The controller, shader, materials and gameplay code retain their prior contents. The previous sections record earlier presets. The initial left-and-down preset used the values below. See the latest entry for the current scene.

| Inspector control | Prior X | Saved X | Prior Y | Saved Y |
| --- | ---: | ---: | ---: | ---: |
| Bend or Vertical Bend | +0.085 | **−0.075** | 0 | **0.45** |
| Bend Start | 12 m | **12 m** | 16 m | **24 m** |
| Bend Entry Distance | 46 m | **34 m** | 32 m | **42 m** |
| Bend Exponent | 3 | **3** | 3 | **3** |
| Maximum Angle | 8° | **8°** | 8° | **8°** |
| Entry Softness | 0.60 | **0.60** | — | — |

The left bend reaches half its effective 6° heading about 46 m in front of the runner. The downhill bend reaches half its 3.6° pitch about 66 m ahead in Y-only. The 20 m difference between these midpoints lets lane markings lead left before the drop becomes visually obvious. X and Y still begin at their configured 12 m and 24 m. Test previews included X −0.065 with Y 0.40 and later entries (too subtle behind the first obstacle group), plus X −0.085 with Y 0.55 and earlier entries (the distant obstacle group dropped too soon for the desired staging). The saved values are the middle preview.

### Reference observations

- SYBO's official Las Vegas home-page gameplay, **00:01.750–00:03.250**, especially **00:02.500**, keeps the rail and pickup path around the runner readable and lets the rails lead left beyond the cars. At **00:06.500** the interior columns frame a shifting distant path. At **00:07.500–00:08.500** the nearby trains and roofs remain legible while objects farther away compress toward the horizon. [Las Vegas footage, 00:02.500](https://subwaysurfers.com/media/uecfjhk5/las_vegas_hero_video_1920x1080.mp4#t=2.5)
- The official Classic portrait footage, inspected through the mirror, shows the approach from **00:00–00:04**. Near sleepers stay aligned with the runner and first trains while the distant rails and coins sweep sideways. At **00:08** and **00:14** the train roof edge stays readable next to the pickup route. At **00:18–00:22** the near track leads clearly through the tunnel approach. [Official portrait gameplay, 00:02](https://www.youtube.com/watch?v=QPW3XwBoQlw&t=2s) · [Mirror, 00:18](https://vimeo.com/1151359249#t=18s)
- A contemporary portrait still shows the near lane rails staying near-vertical while the far corridor sweeps left behind the first obstacle. The combined footage and still informed the composition. The apparent falloff of the distant route is a visual cue. Its pitch, distances, and any overlap with camera motion cannot be measured reliably from these views. The later descent and its numeric settings are tuning choices for this project's assets. [2013 gameplay still](https://europe1.discourse-cdn.com/unity/original/4X/7/e/b/7ebc62f169f7d5b88e5ebb6f1ddc6c70d1ff0ad5.jpeg)

### Repeat the check

1. Load `M7MD_Albitar` in the Windows editor. Select Main Camera. Set Mobile quality and a tall portrait Game view, then enter Play Mode.
2. Pause before the first lane change. From the same camera pose compare the prior settings in the table, the saved X settings with Y strength 0, the saved Y settings with X strength 0, and both saved axes together. The test used 224 × 460 pixels. All four cases had the same camera projection and restored shader globals and culling after rendering. The foreground stayed steady while both-axis markers and curb followed one leftward bend. Use temporary Play Mode inputs for comparisons.
3. Start the normal-speed run and swipe left before an open ramp. The tested layout was Enviroment (02), (03), (1), (04). Nine captures covered 0.29 to 5.545 s at about 20 m/s. The runner stayed on the ground (StreetCollider) through 1.505 s, climbed the ramp at 2.065 s, and reached the SingleBus roof at 2.395–2.695 s. After descending, the run stayed active through chunk replacement at 5.545 s. Nearby lane paint, curb and roof stayed aligned through that run, and the distance dropped gradually. A close coin briefly filled the view around 4.46 s. At that distance it lies in the undeformed foreground. Obstacle layouts may vary after restart.
4. Return to Edit Mode. The scene was reopened and read back with the saved Inspector values and no dirty flag. These checks ran in the Windows editor. Phone appearance and performance have not been measured.

The project inventory and candidate gathering used GPT‑6.2 Sol at low effort. Reference and curvature analysis used GPT‑6.2 Astra at high effort. Tuning and verification preparation used GPT‑6.2 Sol at medium effort, and the primary GPT‑6.2 Sol session owned the Unity Editor, saved the scene values and documented the tests. Changes remain local and uncommitted.


## Earlier and softer left entry

In M7MD_Albitar on Main Camera, X now starts at 8 m (previously 12 m), takes 38 m to half heading (previously 34 m), and uses 0.70 Entry Softness (previously 0.60). The half-heading point stays at 46 m from the runner. X strength remains −0.075, exponent 3, maximum 8°. The saved vertical preset remains 0.45, start 24 m, entry 42 m, exponent 3, maximum 8°. The initial yaw and curvature are zero at the join. The evaluated centerline moves roughly 1 cm left by 20 m and 9 cm by 30 m.

On Mobile portrait, compared the old profile with starts 8 m and 6 m and multiple entry distances. Selected 8 m / 38 m / 0.70 because it stays close to the original distant turn. The camera projection matched in the paused ground and roof comparisons. The normal run crossed into the right lane, climbed the ramp at runner y=0.66 m and reached the bus roof at y=3.40 m with a DualBusses collider directly below. The lower third of the paired roof frame was pixel-identical. Reopened the scene and verified the three values persisted. Tests ran in the Windows Unity Editor. Only the three horizontal Inspector values and this handoff were saved in this pass.

GPT-6.2 Astra at high effort reviewed the curve numerically. GPT-6.2 Sol at medium effort reviewed the verification approach. The primary GPT-6.2 Sol session tested and saved the preset.
