# Game Instructions

BossNPC is a research prototype for NPC memory retrieval strategies in turn-based games. The officially released game version uses the `Experiment` mode by default, and participants do not need to select technical conditions within the game.

## Release Versions

1. Launch the game and complete the first three battles as normal. The system will build a long-term narrative memory based on the actual battles and post-battle choices.
2. Before the first Boss battle, enter `A`, `B`, or `C` according to the grouping code provided on the survey page. Please do not close the survey page.
3. The next three Boss battles will run three different experimental retrieval conditions, but the specific order is determined by the grouping code.
4. After each Boss battle, return to the survey page to complete the corresponding evaluation before continuing the game.

Release builds consistently use `Experiment`. `Scripted` and `Full Memory` are development and baseline testing options, and are not available for switching in the player interface.

## Change Mode in Unity

Project used Unity `2022.3.62f2`.

1. Git clone to local directory.
2. Open it as local project by Unity Hub.
3. Open `Assets/Scenes/GameStart.unity` scene.
4. Find component `GameManager`, which is in --pressistent-- GameObject in scene hierarchy.
5. Modify `Run Mode`：

- `Experiment`: After the first three normal battles, three boss battles follow, using A/B/C grouping to assign similarity-only, rule-based importance, and model-assisted importance conditions.
- `Full Memory`: After the first three normal battles, the original boss scene appears; the dialogue model acquires complete long-term plot memory, without performing embedding or Top-K retrieval.
- `Scripted`: After the first three normal battles, the original boss scene appears; the boss reads fixed lines from localized text, without invoking memory retrieval or dialogue LLM.

Each of the three modes has its own independent `BattleConfig` and scene name array. The default flow is already filled into `GameManager`. Before creating the official release, ensure that `Run Mode` is set to `Experiment`.

## Single Scene Running

When directly opening the `Boss`, `Boss1`, `Boss2`, or `Boss3` scene, set the `Run Mode` in `SingleGameManager`. All three `Battle Config by Run Mode` slots are already populated with the corresponding configuration for that scene:

- `Experiment` uses the experimental conditions specified by `Standalone Experiment Condition`, suitable for testing a single retrieval strategy; this option only includes the three official retrieval strategies.
- `Full Memory` forces the use of full long-term memory.
- `Scripted` forces the use of localized fixed dialogue.

`SingleGameManager` is only used for debugging independent scenes and does not execute in proper order.

## Prerequisites

- `Scripted` does not need LLM or embedding service.
- `Full Memory` requires dialogue generation proxy, but not require embedding interface.
- `Experiment` requires both LLM and embedding.

Project has an uniform entrance for two API, check 'Proxy' folder, open '.env' in text file.

Before launching the game, please run Node.js service in game directory.

Embedding service is prebuilt in game repository, while LLM API link has to be set by user.
