/// MASTER TEMPLATE FOR ALL ASSIGNMENTS
/// 
#import "@preview/wordometer:0.1.5": word-count, total-words
#import "@preview/cetz:0.3.4": canvas, draw

//  FONTS 

#let font-body = "Garamond Premier Pro"
#let font-raw = "Iosevka"
#let font-display = "Garamond Premier Pro"
#let font-heading = "Garamond Premier Pro"
#let font-footer = "Iosevka"

#let col-muted     = rgb("#6e6256")
#let col-rule      = rgb("#c2b9ac")
#let col-surface   = rgb("#f5efe4") 

#let col-on-dark   = rgb("#d8d0c5")  

#let col-a         = rgb("#3e5e35") 
#let col-b         = rgb("#7e3535")
#let col-c         = rgb("#2f5268")
#let col-d         = rgb("#7a5520")

#let col-link      = rgb("#1a3070")
#let col-code-bg   = rgb("#ede5d8")
#let col-block-bg  = rgb("#e6dccb")
#let col-active    = rgb("#dfd0b5") 


#set page(
  paper: "a4",
  margin: (left: 1.8cm, right: 1.8cm, top: 1.8cm, bottom: 1.8cm),
)

#set par(justify: true)
// #set par(leading: 0.6em)

#show raw: set text(font: font-raw, size: 9pt, weight: 400)

#set text(font: font-body, size: 13pt)

#show raw.where(block: false): box.with(
  fill: col-code-bg,
  inset: (x: 3pt, y: 0pt),
  outset: (y: 3pt),
  radius: 0pt,
)

#show raw.where(block: true): block.with(
  fill: col-block-bg,
  inset: 10pt,
  radius: 0pt,
)

#show link: set text(fill: col-link)
#show heading: set text(font: font-heading, weight: 800, size: 1.03em)
#show: word-count.with(exclude: (heading, <no-wc>, figure))

#let module_code = "PROG7311"
#let module_name = "GLMS"
#let assignment = "Portfolio Of Evidence (Final part)"

#let authors = (
  (name: "Jude Fanner", number: "ST10438340"),
)

#let footer-mode = state("footer-mode", "body")

#let page-footer() = context {
  let mode = footer-mode.get()
  if mode != "none" [
    #set text(size: 9pt, weight: 400, fill: col-muted, font: font-footer)
    #authors.at(0).number | #module_code | #assignment#if mode == "body" [ | #total-words words] | #datetime.today().display("[day] [month repr:long] [year]")
    #h(1fr)
    #text(size: 9pt, weight: 400, fill: col-ink)[#counter(page).display("1")]
  ]
}

//  COVER PAGE 
#block(height: 100%, width: 100%)[

  #v(15%)
  #set align(left)

  //  TITLE BLOCK 

  #text(
    40pt,
    weight: 900,
    font: font-display,
    tracking: -0.02em,
  )[#module_code]

  #v(-30pt)

  #text(
    22pt,
    weight: 600,
    font: font-display,
    fill: col-muted,
    tracking: -0.02em,
  )[#module_name]

  #v(-10pt)

  #text(
    17pt,
    weight: 600,
    font: font-display,
    fill: col-muted,
    tracking: -0.02em
  )[#assignment]

  #v(-10pt)

  #v(2em)
  #line(length: 100%, stroke: 1.5pt + col-ink)
  #v(2em)

  //  RIGHT COLUMN INFORMATION 

  #align(right)[

    #set text(font: font-footer, size: 13.5pt, tracking: -0.02em)

    // AUTHOR BLOCK
    #grid(
      columns: (auto, auto),
      column-gutter: 2em,
      row-gutter: 0.6em,
      align: left,

      [*Author*], [*Student Number*],

      ..authors.map(a => (
        [#a.name], [#a.number]
      )).flatten(),
    )

    #v(1.2em)

    // subtle divider
    #line(length: 60%, stroke: 0.7pt + col-rule)

    #v(0.8em)

    // METADATA BLOCK
    #set text(size: 12pt, fill: col-muted)

    #grid(
      columns: (auto, auto),
      column-gutter: 2em,
      row-gutter: 0.4em,
      align: left,

      [Word Count], [#total-words words],
      [Date], [#datetime.today().display("[day] [month repr:long] [year]")],
    )
  ]

  #v(1fr)
] <no-wc>

//  TABLE OF CONTENTS 

#pagebreak()

#set align(left)
#show link: underline

#outline(
  title: "Table of Contents",
  depth: 3,
) <no-wc>

#pagebreak()

#set page(
  numbering: "1",
  number-align: right,
  footer: page-footer(),
)


= Report

// DevOps & Testing: Explain why Automated Testing is critical in a CI/CD pipeline. How does  it prevent bugs from reaching production? 
== Why Automated Testing is Critical in a CI/CD Pipeline
Automated testing lets developers verify that new features and bug fixes do not introduce regressions before code reaches production @testlio2024.
Each commit triggers the test suite, so defects are caught at the point of introduction rather than during later integration @testlio2024.
As release frequency increases, teams that rely on manual regression testing tend to shortcut the process, which allows bugs to reach end users @functionize2018.


// Containerization: Discuss how Docker ensures consistency across Dev, Test, and Prod  environments, solving the "it works on my machine" problem.
== Why Docker Solves the "It Works on My Machine" Problem
A Docker container packages an application together with all its dependencies,
so the same image runs identically in development, test, and production @docker2026.
Developers write code locally using containers, push those containers to a test
environment for automated and manual testing, and promote the same image to
production when testing is complete @docker2026.
Because the running unit never changes between stages, environment-specific
configuration differences cannot cause divergent behaviour, so the "it works
on my machine" problem does not arise @docker2026.

github link: https://github.com/EMWCCN/prog7311-poe-part3-UR91K

#pagebreak()

#footer-mode.update("refs")

#[
#bibliography("references.yml", style: "harvard.csl", title: "References")
#pagebreak()

#footer-mode.update("none")

= Appendix A - Disclaimer

This paper was typeset using Typst (#link("https://typst.app/")[typst.app]), a markup-based typesetting system. The result may look more formally typeset than a typical Word submission; that is the nature of Typst. The source file is available on request.
] <no-wc>
