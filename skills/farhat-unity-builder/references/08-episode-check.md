# Episode check

31 of the 52 chapters are `Inferred`: their story comes from titles, series-level summaries or partial synopses. The episode check turns
`Inferred` into `Verified`. It is the one task the agent cannot finish alone, because it needs someone to watch the licensed episodes.

## 1. What the agent does

1. Generate the worklist: `python3 scripts/story_report.py --out Docs/episode-check.md --csv Docs/story-report.csv`. Season 2 chapters first: they have the least verified content.
2. Prepare, for each chapter on the list, a one-page check sheet in `Docs/episode-check/chNN.md`: the chapter's current `spec` text, the questions to answer, and empty answer fields for the human. Questions are the ones in `references/bible/17-open-items-and-episode-check-list.md` plus these for every chapter: where does it take place; who appears; what is the goal; how does it end; what changes for the characters; which fragment, ability or item is gained.
3. Offer research as a help, not a source of truth: you may search the web for synopses, but anything you find is **agent-researched, not verified**. Put it in the check sheet under "Agent research (unconfirmed)" with the URL, and never copy it into `chapters.json` as verified.
4. Wait for the human's answers. Do not guess missing answers.

## 2. What the human does

Watch the episode (or read a synopsis they trust) and fill in the check sheet, or paste the answers into the chat.

## 3. Applying an answer

For each answered chapter, in this order:

1. Edit `data/chapters.json` for that chapter: `spec.*` text (the bible's five lines for Season 1, three lines for Season 2), `location`, `setPiece`, `coreMechanic`, objectives, unlocks if the episode changes them. Keep objective ids stable where the objective still exists.
2. Set `sourceTag` to `Verified` and `bibleTag` to `V` only if the whole chapter is now confirmed. Remove the chapter from `provisionalUnlocks` entries that were confirmed.
3. Update `references/bible/` text for the chapter and the open-items table so the documents and the data agree.
4. Run `python3 scripts/validate_chapters.py`. Update the expected verified list in `scripts/_common.py` (`EXPECTED_VERIFIED`) and in the Unity test `ChapterDataTests.VerifiedChapters`, because those tests exist to catch accidental retagging. Update the counts in `tests/test_chapters.py`.
5. Run `python3 -m unittest discover -s tests`.
6. Copy `data/chapters.json` into the game project (`scaffold_project.py` does it, or copy by hand), re-import chapters in Unity, then fix the chapter's level to match the confirmed story.
7. Run `progress.py status`: the chapter can now reach `lock` after its stages.

## 4. Open questions that affect more than one chapter

| Question | Where it matters |
| --- | --- |
| Where and in which chapter fragments 1 to 3 and 7 are retrieved | `fragmentPlacement` in `chapters.json` (provisional: 2 in chapter 6, 3 in chapter 12, 7 in chapter 24; 4 in chapter 15); the validator and `ChapterDataTests` assert exactly one owner per fragment, so move the unlock, not duplicate it |
| In which chapter Tilda formally joins the party | Party rules in chapters 3 to 8 |
| Laylah's first appearance and arc | Chapters 28 to 52 |
| How the pulverised Shariman is used and what the missing grain is | Chapters 29, 33 to 35, 38, 52 and the grain resource |
| The role of Tabagee | Possible minor NPC |
| Order of scenes in chapter 2 | Chapter 2 beat sheet |
| Resolution of Farhat's countermeasure in chapter 20 | Chapter 20 |

## 5. Rules

- A chapter is `Verified` only when a human has confirmed it against the episode.
- Do not retag chapters to make a release build pass.
- Keep the check sheets: they are the audit trail for the licensor.
