# Project Belfalas

The goal of this project is to create an app that gamifies the
tasks and actions of daily work life, showing the evolution of
the year through a virtual world that grows as more tasks are
completed.

The tentative tech stack currently planned is:
- Backend in C#, using Entity Framework and a HTTP API
- Frontend in Typescript
- Persistence via a Postgres database
- All the components deployec by Docker Compose, with volumes for any persistent data

This stack can be discussed for alternatives or improvementas as the
requirements become clearer.

Belfalas has two main areas of focus:

## Points and quests

Progress is divided on "areas of focus", like:
- Work projects
- Social interaction
- Side activities

Each area can have a list of actions that can be completed. These
actions are organized in "daily quests" and "weekly quests".

<TO_BE_DEFINED>
- The relationship between daily and weekly quests.
- How to score completed actions: one global currency or different ones?
- How to select which specific actions are part of a dail or weekly set?
- Are quests global, or different sets per area of focus?
</TO_BE_DEFINED>

Assuming 50 weeks per year (excluding holidays), the user could then
progress up to "level 50", potentially gaining a maximum of one level
per week.

<TO_BE_DEFINED>
- All the levels require the same amount of experience?
- Are all the quests needed to gain a level, or does the user have a margin to choose a subset of quests?
</TO_BE_DEFINED>

Each area of focus stores progress individually, but the user only sees
a global level, calculated as the sum of each individual level.

The progress of each area can be inferred by the user through the
evolution of the world, explained in the next section.

<TO_BE_DEFINED>
- Include a catch-up mechanism to recover from slow weeks?
- How to indicate that an area has reached its maximum level? Stop giving quests related to it?
- How to encourage the user to progress without shaming them for failure?
</TO_BE_DEFINED>

At some point (normally by the end or the beginning of a year), the user
can go to the "admin panel" and start a new "era". This process involves:
1) Recording the progress of the current era in a persistent location
2) Creating a new era, potentially with different areas of focus
3) Defining the style or settings of the new daily/weekly quests

The following assumptions can be shared by all eras:
- Each era has at least one area of focus
- Each area of focus has at least enough quests for a full daily and weekly set
- Level 50 is the maximum score for the era

<TO_BE_DEFINED>
- Are all areas of focus of equal importance?
</TO_BE_DEFINED>

## World evolution

The main visualization surface of Belfalas is a "virtual world", that is made up
of a "background/terrain" over which "buildings" and "denizens" are layered.

<TO_BE_DEFINED>
- Is the world a 2D isometric map made of sprites, or a 3D world with models?
- Is the world unique, or can different eras use different templates? (like a tropical world, a fantasy world, a sci-fi world, ...)
</TO_BE_DEFINED>

Ideally the world would be divided into districts or regions, each matching a
different area of focus. This way, the progress can be viewed easily by the user.

Progress in the world should happen at regular intervals, potentially at each
new level (so imagine 50 different stages of evolution). Possible progress can be:
- A new building is added to an empty plot of the world. Plots are likely defined in advance when designing the world template.
- A new denizen is added. Denizens could use a random socket system, that allows them to appear at random locations each time the user opens the world.

<TO_BE_DEFINED>
- Do buildings and denizens have different triggers to appear, or is the order random?
- Can denizens be animated, like moving between spots or doing some emote action?
</TO_BE_DEFINED>

The evolution order of a world template could be static (at least for the buildings), or
could follow a semi-random rule to simulate organic growth (for example, choose a random
plot next to an already-built one). This evolution must persist as long as the era is
active.

Denizens, on the other hand, are more dynamic. They could appear next to buildings, or
doing random activities in the world. Their position and action is not persisted, only
their number and identity (for example, 3 blue denizens, 2 yellow ones and 5 cats).

### Buildings and plots

The world map will have a number of plots (up to 50, if we consider one building
per level). Each plot has a category (like "plot.medium" or "plot.large.lshape").

The category is then mapped to a selection of specific sprites or models that fit
in the shape of that plot (for example, different colors of a house).

Whichever variant is chosen randomly to occupy a plot is then persisted for
the duration of the era.

### Interaction

The world itself is not interactive, the user can only move the camera in the XY axis
and zoom in/out (optional), like in an RTS game. Other than that, they can't edit or
influence the world by any means other than completing quests and gaining levels, which
makes new entities appear in the map.

### New eras and persistence

The visual state of an era is persisted as well when archiving it. In this way, the
user can navigate and view past eras through some historical selection menu.

However, these past eras cannot be altered anymore.
