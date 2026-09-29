# Jev liquidation — three axes, no fields

A shop owner wants to know which stagnant products to discount. The answer changes completely
depending on *why* — who is coming in, what the garment is for, or what time of year it is.

The catalogue has **no audience field, no usage field and no season field.** Nothing in the
data says which customer a product targets or when it sells. So for each run the model gets
the same numbers, one brief in plain language from the owner, and has to work the missing axis
out for itself.

Three runs, 2026-09-29 · branch Campero · window 90d · one request per run
Bands: `>= 0.7` discount · `<= 0.4` keep · in between human review.

| Case | Axis | Objective | What the model had to infer | Score |
|---|---|---|---|---|
| demographic | who is coming | attract_customers | customer type → brand → product | 12 / 2 / 1 |
| use | what it is for | rotate_slow_stock | material and function, inside a category | 12 / 2 / 0 |
| season | when it sells | rotate_slow_stock | garment season, from a category that does not hold it | 13 / 1 / 0 |

No season field, no usage field, no audience field in any catalogue. Every axis was read off
the product name, the category and the brand. The request files and the brief structure are
in "How the request is built" below.

---

## How the request is built

Three layers. The client only writes the first one; the other two are generated.

### 1. What the client POSTs — `POST /api/liquidation/advise`

`Documentation/carousel/test-liquidation-request*.json` — 3 files, one per axis.

| Field | Type | Notes |
|---|---|---|
| `dataset` | enum | `default` · `seasonal` · `workwear`. Selects the fixture. |
| `windowDays` | int | 1-365. How far back sales are measured. Default 90. |
| `eventDate` | date? | The date the clearance is for. Drives the WHEN slot. |
| `objectiveType` | enum? | `attract_customers` · `free_up_space` · `rotate_slow_stock` · `release_cash` |
| `marginFloor` | enum? | `no_loss` · `loss_up_to_10` · `loss_up_to_fifteen` · `get_rid_of_it` |
| `userNotes` | string? | **The brief.** Structured as below. |

**The five typed fields are not repeated in the notes.** Objective and margin floor are
already machine-readable; restating them in prose dilutes them.

### 2. `userNotes` — the five slots

This is the only field the owner writes. It goes in verbatim into
`state.context.user_notes`. Structure, in a shopkeeper's voice:

| # | Slot | Required | Question it answers | From the demographic brief |
|---|---|---|---|---|
| 1 | **WHEN** | yes | when does this happen? | *"in ten days the university semester starts"* |
| 2 | **WHO** | yes | who do I want in the store? | *"the students... from other provinces"* |
| 3 | **WHAT they want** | no | what are they looking for? | *"the loose stuff, the oversized tees"* |
| 4 | **DO** | yes | what do I do? | *"prioritize the young people's clothes that are stagnant"* |
| 5 | **DON'T** | **min. 2** | what do I leave alone? | *"leave the adult clothes alone"* / *"don't touch the kids clothes"* / *"the star products... leave those alone"* |

Rules that came out of the runs:

- **An affirmative instruction alone does not hold.** With slot 4 and an empty slot 5, the
  model anchored on the base rate of the list — 8 stagnant youth products against 5 adult ones
  — and "prioritize the stagnant young products" quietly became "prioritize the stagnant
  products". 5 of 5 adult items flagged wrong.
- **The DON'Ts must be named by category the owner owns**: demographic, product category,
  channel. Naming actual entities is also allowed and it works, but it backfires — naming
  brands to discount created a whitelist that then protected other brands of the same age
  group. Brand names in a DON'T are a last resort.
- **Each DON'T needs a reason.** *"leave the adult clothes alone, they move fine even when it
  doesn't look like it"* beat a bare *"not adult clothes"*. The reason is what the model uses
  when the rule is tested against an edge case.
- **The objective raises the baseline, the brief sets the ceiling.** Same 15 products,
  objective changed to `free_up_space`: 14 of 15 flipped to keep, the adult items rose from
  0.10-0.11 to 0.21-0.27, and none crossed the band because the brief said leave them alone.
  Both halves are required.
- **A narrow DO clause overrides the general logic.** In that run the brief narrowed the DO
  to "scarves and heavy stuff", and the model protected everything else. Expectations have to
  account for that before the run, not after.

### 3. What Jev receives — generated

`Documentation/carousel/ejemplo-peticion.json`, the full payload.

**`state.context`** — snake_case, generated from the typed fields plus the notes verbatim:

```
branch · date · window_days · event_date · days_until_event
objective.type · margin_floor · user_notes
```

**`state.products[]`** — the catalogue. Aggregates are per product, variants are for the size
curve and are never asked about:

```
id · code · name · brand · category · gender · days_in_stock · total_stock
sales_90d_total · margin_90d · return_rate_pct · weeks_of_cover
  └─ variants[]: sku · color · size · stock · sales_90d · price · cost · margin_pct
```

**`questions`** — one per product, keyed by product id, `type: "noul"`. This is the OOL-style
rubric shape, and the criteria are written from the brief's own logic so the model is scored
against the owner's intent rather than a generic discount question:

```
"should a discount be applied to products[i] (CODE Name) as a whole?"
true:  a discount would attract the buyers described in context.user_notes
       and is within the margin floor
false: a discount would not attract those buyers, or it would break the margin floor
```

`as a whole` is deliberate. A markdown on one size breaks the size run, so the decision is at
product level and the variants stay in the state as evidence.

One request, one model call. Input runs 6.4k-6.6k tokens, output ~250, well inside the 32k
state+question limit.

---

## Case 1 — demographic

> `test-liquidation-request.json` · dataset `default` · 15 products / 29 variants · 6,598 input tokens

**Asked.** The university semester starts in 10 days and students arrive from other
provinces. Discount the stagnant youth clothing, leave adult clothes, kids clothes and the
star products alone.

**Why an LLM.** The catalogue has **no audience field**. Nothing says who buys a Ralph
Lauren, so the model has to map *customer type → brand → product* on its own. A rule engine
can only filter on fields that exist, and this one does not exist.

| Product | Brand | Cover | Sold | Decision | Prob | |
|---|---|---:|---:|---|---:|---|
| Denim Overshirt | ZARA | 90 wks | 2 | **discount** | 0.75 | ok |
| Graphic Cotton Tee | H&M | 45 wks | 5 | **discount** | 0.75 | ok |
| Printed Oversized Tee | SHEIN | 60 wks | 3 | **discount** | 0.88 | ok |
| Ribbed Crop Top | SHEIN | 70 wks | 4 | review | 0.45 | soft |
| Washed Denim Jacket | GUESS | 55 wks | 6 | review | 0.43 | soft |
| Slim Fit Chino | DIESEL | 100 wks | 3 | keep | 0.17 | ok |
| Supima Crew Neck | UNIQLO | 40 wks | 8 | keep | 0.31 | ok |
| Basic Cotton Tee | NIKE | 3 wks | 120 | keep | 0.05 | ok |
| Icon Flag Tee | TOMMY HILFIGER | 2.6 wks | 95 | keep | 0.09 | ok |
| Oxford Shirt | RALPH LAUREN | 48 wks | 6 | keep | 0.08 | ok |
| Chino Trouser | RALPH LAUREN | 130 wks | 1 | keep | 0.11 | ok |
| Cotton Pique Polo | LACOSTE | 150 wks | 1 | keep | 0.10 | ok |
| Linen Shirt | TOMMY HILFIGER | 120 wks | 2 | keep | 0.11 | ok |
| 511 Slim Jean | LEVIS | 85 wks | 3 | keep | 0.14 | ok |
| Merino Wool Scarf | H&M | 165 wks | 2 | keep | 0.26 | miss |

**12 / 2 / 1.** All five stagnant adult brands protected, at 0.08-0.14.

The two soft cases are the only two womenswear products, and the brief also says *"they don't
want anything tight"*. A ribbed crop top is not a loose garment. The model applied the owner's
own style rule rather than failing to read it.

The miss is a 165-week-old H&M scarf — the most stagnant item in the run — held at 0.26 by the
demographic rule, because a scarf is a year-round accessory and the brief is about the
semester. The season axis is what catches it, and it does, at 0.92 in case 3.

---

## Case 2 — use

> `test-liquidation-request-workwear.json` · dataset `workwear` · 14 products / 28 variants · 6,489 input tokens

**Asked.** Clear the workwear line, the heavy and protective stuff built for the job. Leave
the fashion pieces alone.

**Why an LLM.** The catalogue has **no usage field**, and the axis is not even in the
category — "Canvas Work Jacket" and "Suede Jacket" are both Outerwear. Only the adjective
separates them, so the model needs material knowledge, not a lookup. A keyword match takes
both or neither.

| Product | Category | Cover | Decision | Prob | |
|---|---|---:|---|---:|---|
| **Canvas Work Jacket** | Outerwear | 100 wks | **discount** | **0.90** | ok |
| Cargo Pant | Pants | 90 wks | **discount** | 0.87 | ok |
| Safety Boot | Boots | 130 wks | **discount** | 0.89 | ok |
| Thermal Base Layer | Base Layer | 85 wks | review | 0.67 | soft |
| Work Glove | Accessories | 110 wks | **discount** | 0.86 | ok |
| **Suede Jacket** | Outerwear | 95 wks | keep | **0.19** | ok |
| Silk Shirt | Shirts | 80 wks | keep | 0.16 | ok |
| Graphic Tee | Tops | 60 wks | keep | 0.26 | ok |
| Crop Top | Tops | 70 wks | keep | 0.19 | ok |
| Denim Jacket | Outerwear | 75 wks | keep | 0.35 | soft |
| Chino | Pants | 80 wks | review | 0.49 | ok |
| Leather Belt | Accessories | 65 wks | review | 0.57 | ok |
| Basic Cotton Tee | NIKE | 3 wks | keep | 0.09 | ok |
| Sports Sneaker | PUMA | 2.8 wks | keep | 0.11 | ok |

**12 / 2 / 0.** Same noun, gap of **0.71** — the separation the whole run is about.

Honest uncertainty: the three products declared undecidable *before* the run — Chino, Leather
Belt, Denim Jacket — came back at 0.49, 0.57 and 0.35. Two inside the review band, the third
just below it and the highest probability of every non-workwear item in the run. The model
ranked the most borderline garment as the most borderline.

```
workwear   0.90  0.89  0.87  0.86  0.67
ambiguous  0.57  0.49  0.35
fashion    0.26  0.19  0.19  0.16
stars      0.11  0.09
```

---

## Case 3 — season

> `test-liquidation-request-season.json` · dataset `seasonal` · 14 products / 28 variants · 6,468 input tokens

**Asked.** The winter is over in ten days, it's already hot at noon. Bring down the whole
winter line. Leave the year-round stuff and the summer stock, that is about to sell again.

**Why an LLM.** The catalogue has **no season field** and no product name contains a season
word. "Polar Jacket", "Denim Jacket" and "Bomber Jacket" are the same category and share a
noun. The model has to know which garment is which, then reason that winter ending means
summer starting — so the summer items, equally stagnant, must be protected instead.

| Product | Category | Cover | Decision | Prob | |
|---|---|---:|---|---:|---|
| **Polar Jacket** | Outerwear | 100 wks | **discount** | **0.93** | ok |
| Insulated Boot | Boots | 140 wks | **discount** | 0.93 | ok |
| Merino Sweater | Knitwear | 120 wks | **discount** | 0.91 | ok |
| Thermal Leggings | Base Layer | 90 wks | **discount** | 0.92 | ok |
| Wool Scarf | Accessories | 150 wks | **discount** | 0.92 | ok |
| **Denim Jacket** | Outerwear | 60 wks | keep | 0.21 | soft |
| **Bomber Jacket** | Outerwear | 85 wks | review | **0.59** | ok |
| Chino Slim | Pants | 80 wks | keep | 0.09 | ok |
| Cardigan | Knitwear | 70 wks | keep | 0.29 | ok |
| Linen Shirt | Shirts | 90 wks | keep | 0.14 | ok |
| Shorts | Pants | 95 wks | keep | 0.13 | ok |
| Sandals | Shoes | 85 wks | keep | 0.14 | ok |
| Basic Cotton Tee | NIKE | 2.5 wks | keep | 0.08 | ok |
| Sports Sneaker | PUMA | 3 wks | keep | 0.10 | ok |

**13 / 1 / 0.** Best run of the three, and the tightest distribution.

**Three jackets, graded.** Polar 0.93, Bomber 0.59, Denim 0.21 — a gap of 0.72, the largest
in the project. The bomber is the interesting one: it *is* a heavy jacket, and the brief says
*"the heavy jackets... all of that"*. The word points at it, the garment knowledge points away,
and it went to review instead of picking a side.

**Closed-loop inference.** The summer items are stagnant at 85-95 weeks, comparable to the
winter items, and came back at 0.13-0.14 while the winter items came back at 0.91-0.93.

```
winter      0.93  0.93  0.92  0.92  0.91
bomber      0.59
year-round  0.29  0.21  0.09
summer      0.14  0.14  0.13
stars       0.10  0.08
```

---

## The same product, three different answers

Two products appear in more than one dataset, on a different axis each time. Both moved.

| Product | Case | Axis | Prob |
|---|---|---|---:|
| Denim Jacket | seasonal | season | 0.21 |
| Denim Jacket | workwear | use | 0.35 |
| Chino Slim | demographic | demographic | 0.11 |
| Chino Slim | seasonal | season | 0.09 |
| Chino | workwear | use | 0.49 |

Same names, same stock, three briefs, a range of 0.09 to 0.49. The data alone would have
produced one number every time.

## What the catalogue does not have

| Field | Present? | Consequence |
|---|---|---|
| Audience | ❌ | inferred from brand + name |
| Season | ❌ | inferred from name + category |
| Intended use | ❌ | inferred from name + material vocabulary |
| Price | ✅ | 40-520 Bs across the three datasets |
| Size curve | ✅ | per variant, never asked about |
| Margins | ✅ | per variant |

---

## Open items

- **n=1 per configuration.** Run-to-run noise is ±0.04, measured on one item across three
  calls. The conclusions here survive that — the gaps are 0.4 to 0.7 — but the exact numbers
  would not reproduce to three decimals.
- **Still fixtures.** No database read anywhere in this series. Every product is synthetic.
- **Two of four objectives measured.** `attract_customers` and `rotate_slow_stock`.
  `free_up_space` and `release_cash` untested.
- **Crossed axes untested.** No garment is both out of season *and* adult-targeted. That is
  where a conflict between the brief's own rules would become visible.
- **Kids exclusion unverifiable.** The demographic brief excludes children's clothing, but
  the demographic dataset has no children's products. It can only be confirmed as not causing
  harm.
- **Audience confidence never crossed back into the flow.** An isolated probe rated twelve
  brands by typical customer age: Ralph Lauren 0.96 for 35+, Lacoste 0.46 with the model
  explicitly uncertain, SHEIN as teens. The confidence tracked real-world ambiguity, which is
  knowledge rather than confabulation — but nothing in the liquidation flow consumes it yet.
