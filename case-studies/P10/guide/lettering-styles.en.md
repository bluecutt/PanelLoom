# Lettered Bubbles: Prompts and Styles

[中文](lettering-styles.md) | [English](lettering-styles.en.md)

Generate lettering, contour, and tail together for each independently movable object. Panel artwork stays separate, so a text revision only replaces its corresponding object. Captions, thoughts, and unboxed text use the same object layer.

This chapter pairs real P10 assets with the wording used to describe their styles. English excerpts come from historical calls or saved generation records. Explanations and the reusable template are editorial material translated from the Chinese guide. The [appendix](prompt-notes.en.md) preserves full historical prompts. Constraints express the intended result; generated glyphs, punctuation, and transparency still require review.

## 1. Define the movable unit

An object may contain one sentence or an intentionally connected bubble group. Decide what must move independently before generation.

C01's three dialogue bubbles move together, so they form one object. U02's thoughts sit on opposite sides of the characters and need separate placement; the final assets are C03A and C03B. Speaker identity alone does not determine object grouping.

| Prompt component | Specify | Effect |
| --- | --- | --- |
| Reference roles | Bubble crop for shape; accepted object for typography | Contour and lettering continuity |
| Object and contour | Single bubble, lobe count, rounded or irregular spikes | Shape and reading rhythm |
| Exact text | Verbatim wording, punctuation, lobe or column assignment | Content and arrangement |
| Tail | Pointed tail or thought beads, direction, actual speaker | Speaker ownership |
| Typography and emotion | Optical size, ink weight, stroke energy, margins, emphasized sentence | Readability and emotional emphasis |
| Transparency | Transparent exterior, white interior; only ink for unboxed text | Clean composition over artwork |
| Output extent | Complete independent object with safe margins | Editing freedom |

Specify the speaker separately from the object's location. A cross-panel thought trail should point to the person thinking, even when another character is closer to the bubble.

## 2. Rounded response and connected dialogue

| C02 · Single response | C01 · Three-part dialogue |
| --- | --- |
| [![C02](../assets/text-objects/C02.png)](../assets/text-objects/C02.png) | [![C01](../assets/text-objects/C01.png)](../assets/text-objects/C01.png) |

### C02: A restrained, readable response

Historical contour and tail constraints:

```text
one isolated medium vertical ordinary Japanese manga speech balloon
calm rounded silhouette
one short pointed tail extending to the right
```

Typography constraints:

```text
elegant, soft, restrained, stable, and sincere
Consistent optical character size
Comfortable inner margins
```

A vertical oval and short pointed tail suit quiet dialogue. Even character size, lighter ink, and generous inner margins make a short line stable to read. Slight hand-drawn asymmetry keeps the contour natural.

### C01: Three sentences with small emotional changes

Historical structural constraints:

```text
exactly three softly connected rounded lobes
arranged diagonally from upper-right to lower-left
one shared short pointed speech tail
```

One sentence goes in each lobe, read upper right → middle → lower left:

1. `也不是.....不行吧.....` — hesitant permission, approximately “It's not that I mind.”
2. `但只准五分钟。` — “But only five minutes.”
3. `你可别想占我便宜。` — “Don't think you can take advantage of me.”

Keep one lettering system, with controlled differences in delivery:

```text
Keep a consistent optical character size and stroke system throughout the connected group
First sentence is slightly tighter and lighter
third is only slightly heavier and firmer
```

The first sentence is lighter and slightly tighter to convey hesitation; the last is a little firmer to express defiance. Ink weight and rhythm carry the emotion while individual character sizes stay consistent.

## 3. Thoughts: Bead trails and cross-panel ownership

| C03A · Right connected pair | C03B · Left single thought |
| --- | --- |
| [![C03A](../assets/text-objects/C03A.png)](../assets/text-objects/C03A.png) | [![C03B](../assets/text-objects/C03B.png)](../assets/text-objects/C03B.png) |

C03 was initially generated as a combined image. It was split into two assets for independent placement, preserving lettering and contour pixels. For new work, specifying separate right and left outputs from the outset establishes that editing boundary during generation.

[![C04 with its upward thought trail](../assets/text-objects/C04.png)](../assets/text-objects/C04.png)

C04 sits beside the A01 flashback, but the thinker is in U02 above. Its record explicitly describes ownership and bead direction:

```text
this thought belongs to adult Subaru in the U02 panel above
Exactly three round thought-tail beads must emerge from the TOP
travel vertically upward/slightly upper-left, decreasing in size upward
```

Three beads become smaller toward the top. Check the direction against final placement so it points to the present-day thinker.

The final text is `真是麻烦的家伙啊.....`, approximately “What a troublesome guy,” with five ordinary final period dots. The historical prompt specified six. The creator later requested five and the extra punctuation was covered locally. Use the final approved wording for a new task; retain the historical prompt unchanged as a record.

## 4. Unboxed vertical text: Ink only

[![C05 two-column unboxed lettering](../assets/text-objects/C05.png)](../assets/text-objects/C05.png)

C05 places two promises in A02 as one text object. Shape and layout constraints:

```text
exactly one isolated transparent text-only object
two separate vertical columns
read the RIGHT column first and the LEFT column second
```

Transparency constraint:

```text
every pixel outside the black lettering must be genuinely transparent RGBA
```

The right column reads `和我成为朋友吧，我会保护你的。` (“Become my friend. I'll protect you.”). The left reads `以后别再一个人躲起来了` (“Don't hide away by yourself again.”). They use the same optical character size; the longer right sentence produces a longer column. Light rays and stars belong to the panel background. This object contains only black lettering, with transparency between glyphs.

## 5. Caption box: Horizontal and steady

[![C06 time caption](../assets/text-objects/C06.png)](../assets/text-objects/C06.png)

Contour and arrangement constraints:

```text
one isolated small horizontal rectangular manga narration box
compact upright Chinese manga narration lettering
horizontally arranged in one centered line from left to right
```

`五分钟后。` (“Five minutes later.”) uses a compact horizontal rectangle, centered upright lettering, and a white interior. It remains an independent editor object that can be placed along the reading route.

## 6. Quiet speech and shouting in one lettering system

| C07 · Restrained reminder | C08 · Emotional peak |
| --- | --- |
| [![C07](../assets/text-objects/C07.png)](../assets/text-objects/C07.png) | [![C08](../assets/text-objects/C08.png)](../assets/text-objects/C08.png) |

C07's `……喂。` (“…Hey.”) uses a small compact rounded bubble and lighter letters:

```text
small compact rounded ordinary manga speech balloon
Small, thin-to-medium, low and restrained
```

C08's `五分钟已经到了吧？！` (“The five minutes are already up, aren't they?!”) is the page's lettering peak. The contour has irregular rounded spikes; the ink is heavier, sharper, and more forward-driving:

```text
lively uneven rounded-spike silhouette
heavier, sharper and slightly forward-driving
express force through stroke pressure, line rhythm and whole-block energy
```

The prompt also requires `consistent optical character size`. Pressure, shape, and overall rhythm create force. Punctuation is explicitly a Chinese question mark followed by an exclamation mark: `？！`. Leave generous inner margins between lettering and spikes.

[![C09 calm connected response](../assets/text-objects/C09.png)](../assets/text-objects/C09.png)

C09's response uses softly connected rounded lobes and restrained asymmetry. Its prompt includes `softly connected rounded two-lobe balloon` and `calm, relaxed, affectionate and completely matter-of-fact`. This creates a quiet contrast to C08 within the same ink system. Its line, `再多抱一会儿。`, means “Let me hold you a little longer.”

## 7. A template for your own artwork

This is the English version of the guide's reusable template:

```text
Generate one independently movable [speech / thought / caption / unboxed text] object.
Reference 1 controls contour, connections, and tail only.
Reference 2 controls the approved lettering and ink style only.
Shape: [contour, lobe count, connection direction].
Exact text: [verbatim wording and punctuation].
Layout: [horizontal / vertical, reading order, lobe assignment, inner margins].
Speaker and tail: [owner, pointed tail / thought beads, direction and target].
Typography: consistent optical size and stroke system. Express [specified line]'s
emotion through [ink weight, stroke gesture, overall rhythm].
Output: high-resolution PNG, genuinely transparent outside; white bubble or box interior.
Unboxed text retains ink only. Leave safety margins around complete tips and beads.
Output this object alone, without characters, scenery, panel borders, or extra text.
```

The historical examples use vertical Chinese lettering. For your own English-language comic, specify horizontal left-to-right text and intended line breaks where appropriate. Decide the panel reading route and the text flow separately, and adjust bubble proportions to your words. The supplied case assets retain their original lettering.

Check wording, punctuation, tail ownership, and the actual alpha channel, then submit for creator review. A checkerboard can be painted into a generated image. Some historical P10 outputs required documented exterior-background alpha processing, with white bubble interiors preserved. Scaling an object also changes its reading size; compare optical character size across neighboring objects during initialization.

Approved assets are imported as complete objects. Placement, clipping, and occlusion are layout controls. A lettered bitmap should not be mirrored, since that would mirror its characters too. The final page comes directly from local export.

[Object list](text-objects.en.md) · [Full historical prompts](prompt-notes.en.md) · [Full workflow](full-workflow.en.md)
