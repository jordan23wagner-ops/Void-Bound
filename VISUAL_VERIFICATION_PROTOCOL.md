# Visual Self-Verification Protocol (CONTEXT.md Addendum)

**Add this as a new standing rule in CONTEXT.md, applying to ANY task involving UI layout, visual styling, sizing, colors, or icons — supersedes the generic Play-Mode self-test for visual work specifically.**

## The Problem This Solves
Code-level self-testing (checking Console errors, querying component values) does NOT 
catch visual bugs — wrong sizes, invisible icons, broken fonts, misaligned panels. 
Multiple rounds of "fixed and verified" have shipped visually broken UI because 
verification only checked that code ran without exceptions, not that the result 
looked correct.

## New Standing Rule: Screenshot-Based Visual Verification

For ANY task that builds or modifies UI/visual elements, before reporting complete:

1. Use Unity MCP to capture an actual screenshot of the Game view in Play Mode 
   (not just query object properties)
2. Look at the screenshot yourself (you have vision — use it) and check it against 
   the written spec point by point:
   - Are panels the expected approximate size relative to screen width?
   - Are all icons/text actually visible (not blank squares, not invisible due to 
     font/color issues)?
   - Do colors match the specified hex values (rarity borders, stat colors, etc.)?
   - Is the layout structure correct (correct column order, correct grouping)?
3. If ANY checklist item fails, diagnose and fix it yourself, then re-screenshot 
   and re-check. Repeat up to 3 times internally.
4. Only report back to Jordon once the screenshot you captured and reviewed 
   actually matches the spec. If after 3 attempts it still doesn't match, report 
   back honestly with the screenshot AND a specific description of what's still 
   wrong — do not report "complete" for something you haven't visually confirmed.

## Test Resolution Standardization
When testing UI sizing specifically, set the Game view to a FIXED resolution 
(1920x1080) rather than "Free Aspect" before capturing verification screenshots — 
Free Aspect scales to the Editor window size, making it impossible to judge whether 
a sizing bug is real or just a small window. Use Unity MCP or the Game view 
dropdown to lock this during your own verification; Jordon can test at Free Aspect 
afterward since the CanvasScaler handles real scaling correctly.

## Icon Rendering — Known Issue, Standing Fix
Unicode symbol characters (⛨ ⬡ ▤ ⚔ etc.) do NOT render in Unity's default TMP font 
(Liberation Sans) — they show as blank squares or invisible glyphs. Going forward, 
for any icon-like UI element:
- Do NOT use Unicode symbol characters in TextMeshPro text fields as a substitute 
  for real icons
- Either (a) use plain ASCII letter abbreviations as an explicitly temporary 
  placeholder (clearly flagged as such), or (b) build actual icon sprites as 
  Texture2D/Sprite assets and assign them to Image components, or (c) import a 
  proper icon font as a TMP Sprite Asset (Unity's supported method for icon fonts 
  in TextMeshPro)
- Verify via the screenshot protocol above that whichever method is used actually 
  renders visibly — don't assume a Unicode character will display correctly
