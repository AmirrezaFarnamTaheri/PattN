# Discovery desktop accessibility validation

Static automation metadata and successful UI builds are prerequisites, not proof of desktop accessibility. Complete this record only with real release-build validation.

## Standards target and claim discipline

- Use **WCAG 2.2 AAA success criteria where they meaningfully map to a native desktop application**, with WCAG-EM 2-style reproducible evaluation records.
- Use the current **WCAG 3 Working Draft only as an experimental quality lens**. It is not a finished Recommendation, and this project must not claim a final Bronze/Silver/Gold WCAG 3 rating from the draft.
- WAI-ARIA applies to web content/applications. WPF and Avalonia must expose equivalent native semantics through Windows UI Automation, Linux AT-SPI, and the platform accessibility bridge used on macOS rather than adding ARIA-like strings to XAML.
- A source review, passing build, automation-name audit, or screenshot is not a conformance result. Record the exact commit, platform, assistive technology, scaling, theme, and observed behavior.

## Required matrix

| Platform | Scaling | Accessibility path | Required evidence |
| --- | --- | --- | --- |
| Windows | 100%, 150%, 200% | Narrator and/or NVDA through UI Automation | commit, OS/build, AT version, notes, screenshots/tree capture where useful |
| Linux release desktop | 100%, 200% where supported | Orca through AT-SPI | commit, distro/desktop, AT version, notes |
| macOS, if the management surface ships there | native/Retina plus enlarged UI | VoiceOver | commit, OS, VoiceOver result, notes |

## Keyboard and focus

- Use no mouse. Reach every actionable control with Tab/Shift+Tab or platform-standard menu/grid navigation.
- Interactive controls added by Discovery/Reviver should provide an approximately 44 x 44 device-independent-pixel target where layout permits; document any compact-data-grid exception and ensure an equivalent keyboard path.
- Verify focus order follows the visible task order and remains predictable after dialogs, refreshes, and tab changes.
- Verify every focused control has a visible focus indicator at every tested scale/theme.
- Verify menus, tabs, expanders, grids, scroll areas, and dialogs contain no keyboard trap.
- Collapsed progressive-disclosure content must not leave descendants in the focus/accessibility tree.
- Destructive or trust-changing operations must not fire from focus movement alone.

## Name, role, state, and announcements

- Inspect interactive controls through the platform accessibility API for meaningful name, role, enabled/disabled state, and checked/expanded/selected state.
- Data grids must expose grid/table semantics, headers, current selection, and cell values in a usable reading order.
- Read-only evidence fields must be distinguishable from editable trust/configuration fields.
- Status updates must be announced without stealing focus or creating repeated announcement storms.
- Validation/stale-plan errors must be discoverable by screen-reader users and associated with the operation that caused them.

## Visual, high-DPI, resize, and disclosure

- Verify normal text reaches a 7:1 contrast target and large text a 4.5:1 target wherever the active theme permits WCAG AAA evaluation; non-text UI boundaries/focus indicators must remain clearly perceivable in normal, dark, and high-contrast/forced-color modes.
- Do not encode health, trust, failure, selection, or destructive state by color alone. Pair it with text, iconography, shape/state, or an accessibility property.
- Verify focus indicators remain visible against adjacent colors and are not clipped by scroll viewers, toolbars, or dialog boundaries.
- Test text-spacing/large-font overrides and localized long strings without loss of controls or information.
- At each tested scale, shrink the window to the minimum practical size and verify that no action becomes unreachable or clipped without a usable scroll path.
- Expand every optional trust/source/maintenance section and repeat keyboard navigation at large scaling.
- Exercise long URIs, hashes, IPv6 addresses, localized strings, and error messages for clipping/overlap.
- When an expander is collapsed, focus must remain on or move predictably from the toggle.

## Completion rule

A platform row is complete only when the exact commit and reproducible evidence are recorded. Any failure becomes a tracked defect with reproduction steps. Do not close accessibility validation from XAML inspection alone.

A release-level accessibility claim additionally requires:
- no unresolved keyboard trap, unreachable action, focus-loss, or clipped-content defect in the tested matrix;
- no critical name/role/state/value defect in platform accessibility inspection;
- screen-reader verification of status/error announcements and destructive confirmation flows;
- high-contrast and 200% scaling evidence for every shipped desktop surface touched by Discovery/Reviver;
- explicit documentation of any WCAG criterion judged not applicable to the native desktop scope.
