# Ideas for Analytics module

## UI and Navigation

The user sees a yearly overview, from January to December (even if
the current year is not yet finished).
In addition, the user can navigate back and forth between years, similar
to other modules.

Most charts or graphs should include the "year over year". This means
that, in addition to the current year N data, the user also sees a second
data series of the same metric in year N-1.
Unless the meaning of a specific chart doesn't need this feature.

### Layout

As there is a large number of charts and data in this module, we may
need to create some sort of tab panel or sub-pages, to group the data
related to each individual module, as well as aggregated data from
more than one module. This needs to be discussed in the planning phase.

If possible, implement a lazy loading system, so that only data needed
by the user is calculated.

## Currency handling

For this version, the easiest way to aggregate amounts between
different currencies is to convert everything into EUR.

We'll need to add a field in the configuration module so that
admins can indicate the conversion between each currency and EUR.
The EUR currency will have a value of 1, no need to treat it
in a special way.


## Charts included

Some of these charts could be combined or redesigned, analyze them and
feel free to propose changes or alternatives.

### Data coming from a single module

1. Capex expenses grouped by category (only completed expenses)
2. Capex incomes grouped by category (only completed incomes)
3. Capex expenses grouped by supplier (only completed expenses)
4. Capex incomes grouped by supplier (only completed incomes)
5. Capex expenses grouped by cost center (only completed expenses)
6. Capex incomes grouped by cost center (only completed incomes)

7. Opex expenses grouped by category
8. Opex incomes grouped by category
9. Opex expenses grouped by supplier
10. Opex incomes grouped by supplier
11. Opex expenses grouped by cost center
12. Opex incomes grouped by cost center

13. Inventory expenses grouped by item category (ignore planning or cancelled orders)
14. Inventory expenses grouped by supplier (ignore planning or cancelled orders)
15. Average order amount grouped by supplier (ignore planning or cancelled orders)
16. Top five items by amount spent in the year (with relative percent among the total expense)
17. Top five suppliers by amount spent in the year (with relative percent among the total expense)

18. Travel expenses grouped by category (ignore cancelled travels)
19. Travel expenses grouped by supplier (ignore cancelled travels)
20. Travel expenses grouped by cost center (ignore cancelled travels)
21. Travel expenses grouped by destination (only for travels that link to a destination, ignore cancelled travels)

### Data aggregated between modules

22. Total expenses grouped by supplier
23. Total expenses grouped by category (categories between different modules are matched by string comparison)
24. Total expenses grouped by cost center
