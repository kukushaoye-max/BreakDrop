# Candy UI suite — generation prompts

Tool: built-in image_gen (not CLI).

Reference: E:/Project/BreakDrop/docs/design/candy-ui-v1/home-gameplay-candy-v1.png

## 02-level-select

```text
Use case: ui-mockup. High fidelity game UI design handoff for BREAK & DROP. Image 1 is ONLY the approved STYLE REFERENCE, not an edit target. Match its strawberry glossy jelly, vanilla frosting, caramel biscuit edging, pastel candy landscape, rounded chocolate lettering, cyan hexagon with white star, warm cheerful toy lighting. Crisp readable English text, clean professional layout, no watermark, no phone hardware, no dashboard styling. This is a tap-to-remove-supports physics puzzle, NOT match-three. Do not introduce currency, hearts/lives, ads, purchases, shops, locks, accounts or new mechanics.
Asset: one complete portrait LEVEL SELECT screen, 9:19.5 portrait composition. Full bleed soft candy sky and candy hills, quiet center. Top frosted biscuit plaque exact text "LEVEL SELECT", upper right small round X close cookie. Subheading "20 PHYSICS PUZZLES". A playful candy tray with 20 large numbered biscuit/jelly tiles in EXACTLY 4 columns and 5 rows, numerals rowwise "01" "02" "03" "04", "05" "06" "07" "08", "09" "10" "11" "12", "13" "14" "15" "16", "17" "18" "19" "20". All 20 are enabled with legible chocolate numerals, NO locks and NO completion star badges (not tracked here). Tile 08 is strawberry jelly with cream numeral and subtle cream outline to indicate current selection; all other tiles vanilla frosted cookies. A small footer "Choose a puzzle". Comfortable finger targets, balanced margins, controls within safe area. No extra buttons or unrelated text.
```

## 03-core-modals

```text
Use case: ui-mockup. High fidelity game UI design handoff for BREAK & DROP. Image 1 is ONLY the approved STYLE REFERENCE, not an edit target. Match its strawberry glossy jelly, vanilla frosting, caramel biscuit edging, pastel candy landscape, rounded chocolate lettering, cyan hexagon with white star, warm cheerful toy lighting. Crisp readable English text, clean professional layout, no watermark, no phone hardware, no dashboard styling. This is a tap-to-remove-supports physics puzzle, NOT match-three. Do not introduce currency, hearts/lives, ads, purchases, shops, locks, accounts or new mechanics.
Asset: wide landscape UI presentation of THREE complete portrait screens side by side, equal scale, narrow cream gutters. Show an elegant large cream-frosted biscuit dialog centered over a dimmed candy game/home background in each. No outside labels.
LEFT: HOME SETTINGS dialog. Header "SETTINGS", little gear cookie medallion, body "Study the supports." then "Choose the right order." A large pink jelly "CONTINUE" button and a secondary cream cookie "BACK TO HOME" button. Do not invent music/haptics toggles; this software has none. Behind modal show blurred HOME logo from reference.
MIDDLE: paused gameplay, header "PAUSED", little pause cookie medallion, body "Study the supports." then "Choose the right order." Primary pink "CONTINUE", secondary cream "BACK TO HOME". Background game tower visible but dimmed, header clock "00:58".
RIGHT: success modal header "YOU WIN!", exactly THREE bright golden stars, small cyan white-star hex resting on mint goal biscuit, body "SAFE LANDING" and "MOVES 08". Buttons vertically "NEXT LEVEL" pink primary, "UNDO LAST MOVE" cream secondary, "BACK TO HOME" smaller cream tertiary. Confetti sparse outside modal. No scores/currencies. All buttons clearly fit within dialog, no clipping. All copy exact and large. The same dialog corners, borders, typography and action dimensions across screens.
```

## 04-failure-modals

```text
Use case: ui-mockup. High fidelity game UI design handoff for BREAK & DROP. Image 1 is ONLY the approved STYLE REFERENCE, not an edit target. Match its strawberry glossy jelly, vanilla frosting, caramel biscuit edging, pastel candy landscape, rounded chocolate lettering, cyan hexagon with white star, warm cheerful toy lighting. Crisp readable English text, clean professional layout, no watermark, no phone hardware, no dashboard styling. This is a tap-to-remove-supports physics puzzle, NOT match-three. Do not introduce currency, hearts/lives, ads, purchases, shops, locks, accounts or new mechanics.
Asset: wide landscape board with THREE equal full portrait gameplay screens side by side, narrow cream gutters. A dimmed candy gameplay scene behind each with clear round cream cookie result panels. Match exact same dialog component across all three. NO rewards or happy confetti. Each has exact header "TRY AGAIN", a small cyan white-star hex medallion and three small progress stars (some muted). Body text and recovery:
LEFT body "The hex left the safe area". Golden first star and two muted cream stars. Buttons "RETRY" pink primary, "UNDO LAST MOVE" cream secondary enabled, "BACK TO HOME" smaller tertiary.
MIDDLE body "Time's up!" plus clear clock "00:00". Two golden stars one muted. Buttons "RETRY" pink primary, "UNDO LAST MOVE" muted disabled gray-lavender (because undo cannot restore time), "BACK TO HOME" cream tertiary.
RIGHT body "No removable blocks remain". One golden star two muted. Buttons "RETRY" pink primary, "UNDO LAST MOVE" cream secondary enabled, "BACK TO HOME" cream tertiary.
Keep text readable with generous whitespace. Ensure these are clearly game over recovery modals, not app error dialogs. No red system-error triangles, no purchases. All screens fit fully.
```

## 05-controls

```text
Use case: ui-mockup. High fidelity game UI design handoff for BREAK & DROP. Image 1 is ONLY the approved STYLE REFERENCE, not an edit target. Match its strawberry glossy jelly, vanilla frosting, caramel biscuit edging, pastel candy landscape, rounded chocolate lettering, cyan hexagon with white star, warm cheerful toy lighting. Crisp readable English text, clean professional layout, no watermark, no phone hardware, no dashboard styling. This is a tap-to-remove-supports physics puzzle, NOT match-three. Do not introduce currency, hearts/lives, ads, purchases, shops, locks, accounts or new mechanics.
Asset: large high resolution landscape UI COMPONENT DESIGN SHEET on flat warm ivory background, NOT a screen mockup. All components clearly separated with ample space, consistent scales, crisp dark labels. Title "BREAK & DROP — UI KIT". Organized five horizontal rows.
Row 1 header "BUTTON STATES": four identical wide strawberry jelly buttons reading "PLAY", with small labels below respectively "DEFAULT", "PRESSED", "FOCUS", "DISABLED". Default juicy bevel; pressed compressed bevel darker pink; focus double cream/cocoa outline; disabled desaturated lavender with still readable cream text.
Row 2 header "ACTION BUTTONS": pink "CONTINUE", pink "NEXT LEVEL", pink "RETRY", cream biscuit "LEVELS", cream biscuit "BACK TO HOME". Not tall cards.
Row 3 header "ICON BUTTONS": eight circular cookie or gummy controls with separate short captions "BACK", "CLOSE", "SETTINGS", "PAUSE", "PLAY", "UNDO", "HINT", "RESTART". Use left chevron, X, gear, parallel bars, triangle, hooked back arrow, lightbulb, clockwise circular arrow. Strong simple cream/chocolate glyphs with 3D candy buttons.
Row 4 header "HUD & STATUS": biscuit "LEVEL 08", clock "00:58", pink warning clock "00:09", small "MOVES 03", golden filled star and muted empty star, two level tiles "08" current pink and "09" cream.
Row 5 header "SURFACES & FEEDBACK": one empty medium cream-frosted biscuit modal panel, one small cocoa dimming swatch, three compact cream rounded toast pills exact texts "Last move restored", "Try the flashing block", "Waiting for the tower to settle".
Maintain material consistency, no invented toggles, sliders, currencies, badges, lock icons or text not requested. All required elements visible, not overlapping.
```

## 06-gameplay-states

```text
Use case: ui-mockup. High fidelity game UI design handoff for BREAK & DROP. Image 1 is ONLY the approved STYLE REFERENCE, not an edit target. Match its strawberry glossy jelly, vanilla frosting, caramel biscuit edging, pastel candy landscape, rounded chocolate lettering, cyan hexagon with white star, warm cheerful toy lighting. Crisp readable English text, clean professional layout, no watermark, no phone hardware, no dashboard styling. This is a tap-to-remove-supports physics puzzle, NOT match-three. Do not introduce currency, hearts/lives, ads, purchases, shops, locks, accounts or new mechanics.
Asset: widescreen UI state design board of FOUR portrait gameplay screens in one row, equal scale, thin cream gutters. Each is complete: biscuit "LEVEL 08" title, back and pause icons; a tightly supported front-on interlocking gummy block tower with visible L shapes, rectangles, squares and empty notches; cyan white-star hex on top in states 1,2,4; mint frosted landing biscuit; three height stars; UNDO, HINT, RESTART round controls bottom. Soft quiet candy environment, no floating random candies obscuring tower.
Screen 1: READY state, clock "00:58", "MOVES 00", center-bottom short helper "Tap blocks. Land safely." Undo button visibly disabled, hint and restart active.
Screen 2: HINT state, clock "00:48", "MOVES 02". Exactly ONE selected pink support block has a thin warm golden outline and sparkle WITHOUT changing its shape or scale. Status "Try the flashing block". Buttons active.
Screen 3: PHYSICS RESOLVING state, clock "00:35", "MOVES 03". One missing support, nearby gummy blocks tilt and fall slightly under gravity, cyan hex descending, small sparse sugar-fragment feedback. Status "Watch the blocks fall..." Hint button muted disabled, Undo and Restart stay active. Not a swipe or grid refill.
Screen 4: LOW TIME state, warning strawberry timer "00:09", "MOVES 04". Otherwise a stable supported puzzle. Short status "Tap blocks. Land safely." Timer warning includes clock shape and stronger outlined border, not color alone; board remains readable and unblocked. No modal or full-screen red flash.
Draw same interface proportions in all four. This is a gameplay state visual specification, no labels outside the screens, all copy concise exact.
```

## Failure timer correction

```text
Use case: precise-object-edit. Image 1 is the edit target, the three candy-style failure UI screens. Change ONLY the small gameplay HUD timer at the very top of the MIDDLE screen from "00:58" to "00:00". The large "00:00" clock inside the middle Time's up dialog is already correct and must stay unchanged. Preserve every other pixel as closely as possible: all three panels, labels, button states, positions, characters, colors, materials, background, all other timers, canvas dimensions and composition. No extra elements, no watermark.
```

