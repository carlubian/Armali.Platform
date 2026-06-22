/* global React */
// Recipes module — the household's cooking module. A recipe collection (each
// recipe records ingredients, ordered steps, a dish image, optional difficulty,
// servings & times) plus weekly menus that lay recipes across a 7-day × 4-slot
// grid. Sample data + small formatting helpers, exposed on window for the other
// babel scripts. No invented colors — everything maps onto Armali tokens.
(() => {

// ── Reference "today" (Europe/Madrid) ───────────────────────────
// Wednesday 24 Jun 2026, which sits inside the ISO week beginning Monday
// 22 Jun 2026 — the week the planner opens on (matching the requirement's
// example route ?week=2026-06-22), so "this week" and the today highlight agree.
const TODAY = new Date(2026, 5, 24);
const MS_DAY = 86400000;
const MONTHS_SHORT = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
const MONTHS = ["January", "February", "March", "April", "May", "June",
  "July", "August", "September", "October", "November", "December"];
const DOW_SHORT = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
const DOW_LONG = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

// ── Catalogue: RecipeCategory (owned by Recipes, managed in Config) ──
// Seeded values from the requirements. Each carries an order; a tone is
// assigned for gentle visual rhythm and a Lucide glyph for the thumbnail.
const REC_CATEGORIES = [
  { value: "Breakfast", order: 1, tone: "gold",    icon: "egg" },
  { value: "Starter",   order: 2, tone: "sea",     icon: "salad" },
  { value: "Main",      order: 3, tone: "aqua",    icon: "utensils" },
  { value: "Dessert",   order: 4, tone: "rose",    icon: "cake-slice" },
  { value: "Drink",     order: 5, tone: "azure",   icon: "wine" },
  { value: "Sauce",     order: 6, tone: "gold",    icon: "soup" },
  { value: "Other",     order: 7, tone: "neutral", icon: "utensils-crossed" },
];
const CAT = Object.fromEntries(REC_CATEGORIES.map((c) => [c.value, c]));

// ── Fixed enum: Difficulty (not a catalogue) ────────────────────
const REC_DIFFICULTY = {
  Easy:   { label: "Easy",   tone: "success" },
  Medium: { label: "Medium", tone: "gold" },
  Hard:   { label: "Hard",   tone: "danger" },
};
const DIFFICULTY_ORDER = ["Easy", "Medium", "Hard"];

// ── Fixed enum: the four meal slots ─────────────────────────────
const MEAL_SLOTS = [
  { key: "Breakfast", icon: "sunrise" },
  { key: "Lunch",     icon: "sun" },
  { key: "Snack",     icon: "cookie" },
  { key: "Dinner",    icon: "moon" },
];

// ── Inventory items (the cross-module reference target) ──────────
// A narrow read view of the Inventory module: pantry / fridge stock that a
// recipe ingredient may optionally link to. Owned by Inventory, not Recipes.
const INVENTORY = [
  { id: "INV-0042", name: "Extra-virgin olive oil", category: "Pantry",  unit: "750 ml bottle", stock: 3, visibility: "Public" },
  { id: "INV-0043", name: "Fine sea salt",          category: "Pantry",  unit: "1 kg box",      stock: 2, visibility: "Public" },
  { id: "INV-0044", name: "Black peppercorns",      category: "Spices",  unit: "100 g jar",     stock: 1, visibility: "Public" },
  { id: "INV-0045", name: "Plain flour",            category: "Pantry",  unit: "1 kg bag",      stock: 4, visibility: "Public" },
  { id: "INV-0046", name: "Caster sugar",           category: "Pantry",  unit: "1 kg bag",      stock: 2, visibility: "Public" },
  { id: "INV-0047", name: "Large eggs",             category: "Fridge",  unit: "dozen",         stock: 11, visibility: "Public" },
  { id: "INV-0048", name: "Unsalted butter",        category: "Fridge",  unit: "250 g block",   stock: 2, visibility: "Public" },
  { id: "INV-0049", name: "Whole milk",             category: "Fridge",  unit: "1 L carton",    stock: 3, visibility: "Public" },
  { id: "INV-0050", name: "Parmesan, aged 24m",     category: "Fridge",  unit: "200 g wedge",   stock: 1, visibility: "Public" },
  { id: "INV-0051", name: "Spaghetti",              category: "Pantry",  unit: "500 g pack",    stock: 5, visibility: "Public" },
  { id: "INV-0052", name: "Arborio rice",           category: "Pantry",  unit: "1 kg bag",      stock: 2, visibility: "Public" },
  { id: "INV-0053", name: "Garlic",                 category: "Produce", unit: "bulb",          stock: 6, visibility: "Public" },
  { id: "INV-0054", name: "Yellow onions",          category: "Produce", unit: "loose",         stock: 8, visibility: "Public" },
  { id: "INV-0055", name: "Tinned plum tomatoes",   category: "Pantry",  unit: "400 g tin",     stock: 7, visibility: "Public" },
  { id: "INV-0056", name: "Fresh basil",            category: "Produce", unit: "pot",           stock: 1, visibility: "Public" },
  { id: "INV-0057", name: "Lemons",                 category: "Produce", unit: "loose",         stock: 5, visibility: "Public" },
  { id: "INV-0058", name: "Dark chocolate 70%",     category: "Pantry",  unit: "200 g bar",     stock: 3, visibility: "Public" },
  { id: "INV-0059", name: "Vanilla pods",           category: "Spices",  unit: "tube of 2",     stock: 1, visibility: "Public" },
  { id: "INV-0060", name: "Chicken stock",          category: "Pantry",  unit: "1 L carton",    stock: 4, visibility: "Public" },
  { id: "INV-0061", name: "Double cream",           category: "Fridge",  unit: "300 ml pot",    stock: 2, visibility: "Public" },
  { id: "INV-0062", name: "Smoked paprika",         category: "Spices",  unit: "75 g tin",      stock: 1, visibility: "Public" },
  { id: "INV-0063", name: "Chickpeas",              category: "Pantry",  unit: "400 g tin",     stock: 6, visibility: "Public" },
  { id: "INV-0064", name: "Greek yoghurt",          category: "Fridge",  unit: "500 g tub",     stock: 2, visibility: "Public" },
  { id: "INV-0065", name: "Honey",                  category: "Pantry",  unit: "350 g jar",     stock: 1, visibility: "Public" },
];
const INV = Object.fromEntries(INVENTORY.map((i) => [i.id, i]));

// ── Builders ────────────────────────────────────────────────────
let _ing = 0, _stp = 0;
// ingredient: free-text name (required), optional quantity, optional item link
const ing = (name, qty, itemId) => ({ id: "ing" + (++_ing), name, qty: qty || null, itemId: itemId || null });
const step = (text) => ({ id: "stp" + (++_stp), text });

// ── Recipe collection ───────────────────────────────────────────
// Mediterranean household cooking. `image` carries a warm two-stop gradient
// so a card with a primary image reads richly; recipes without one fall back
// to a tinted placeholder tile (the Clothes / Assets fallback pattern).
const RECIPES = [
  {
    id: "rcp-carbonara", name: "Spaghetti alla carbonara", category: "Main", difficulty: "Medium",
    servings: 4, prep: 10, cook: 18, hasImage: true, visibility: "Public",
    owner: "Marina Velasco", created: "14 Feb 2026", updated: "4 days ago",
    image: ["#E8B33E", "#C2451F"],
    notes: "No cream — the silk comes from emulsifying the egg with the pasta water off the heat. Work fast.",
    ingredients: [
      ing("Spaghetti", "400 g", "INV-0051"),
      ing("Guanciale, diced", "150 g"),
      ing("Large eggs (2 whole + 2 yolks)", "4", "INV-0047"),
      ing("Parmesan, finely grated", "60 g", "INV-0050"),
      ing("Black pepper", "to taste", "INV-0044"),
      ing("Fine sea salt", "for the pasta water", "INV-0043"),
    ],
    steps: [
      step("Bring a large pan of well-salted water to the boil and cook the spaghetti until al dente."),
      step("Meanwhile, render the guanciale in a cold dry pan over medium heat until crisp and golden."),
      step("Whisk the eggs and yolks with the grated parmesan and a generous amount of black pepper."),
      step("Drain the pasta, reserving a mugful of the starchy water. Toss the pasta with the guanciale off the heat."),
      step("Add the egg mixture and a splash of pasta water, tossing continuously until glossy and creamy. Serve at once."),
    ],
  },
  {
    id: "rcp-tortilla", name: "Tortilla de patatas", category: "Main", difficulty: "Medium",
    servings: 6, prep: 20, cook: 25, hasImage: true, visibility: "Public",
    owner: "Diego Salas", created: "9 Jan 2026", updated: "Yesterday",
    image: ["#EBC15A", "#B8842B"],
    notes: "Confit the potatoes gently in oil — they should steam, not fry. Slightly runny in the middle is the goal.",
    ingredients: [
      ing("Waxy potatoes, thinly sliced", "800 g"),
      ing("Yellow onions, sliced", "2", "INV-0054"),
      ing("Large eggs", "6", "INV-0047"),
      ing("Extra-virgin olive oil", "300 ml", "INV-0042"),
      ing("Fine sea salt", "to taste", "INV-0043"),
    ],
    steps: [
      step("Warm the oil in a deep pan and confit the potatoes and onions over low heat until tender, about 20 minutes."),
      step("Drain well, reserving the oil. Beat the eggs with salt and fold the potatoes through; rest 10 minutes."),
      step("Set a little oil in a non-stick pan, pour in the mixture and cook over medium-low until the edges set."),
      step("Flip onto a plate and slide back to cook the second side. Keep the centre just soft."),
    ],
  },
  {
    id: "rcp-gazpacho", name: "Andalusian gazpacho", category: "Starter", difficulty: "Easy",
    servings: 4, prep: 15, cook: 0, hasImage: true, visibility: "Public",
    owner: "Nora Quintana", created: "2 Jun 2026", updated: "2 days ago",
    image: ["#E07A4A", "#B23A2A"],
    notes: "Best made a day ahead and served very cold. Strain for a silkier texture if you like.",
    ingredients: [
      ing("Ripe tomatoes", "1 kg"),
      ing("Cucumber, peeled", "1"),
      ing("Green pepper", "1"),
      ing("Garlic clove", "1", "INV-0053"),
      ing("Extra-virgin olive oil", "80 ml", "INV-0042"),
      ing("Sherry vinegar", "1 tbsp"),
      ing("Fine sea salt", "to taste", "INV-0043"),
    ],
    steps: [
      step("Roughly chop the vegetables and blend with the garlic until smooth."),
      step("With the motor running, stream in the oil, then add the vinegar and salt to taste."),
      step("Chill thoroughly for at least 2 hours. Serve cold with a thread of olive oil."),
    ],
  },
  {
    id: "rcp-risotto", name: "Lemon & parmesan risotto", category: "Main", difficulty: "Hard",
    servings: 4, prep: 10, cook: 30, hasImage: true, visibility: "Public",
    owner: "Marina Velasco", created: "21 Mar 2026", updated: "1 week ago",
    image: ["#7FBF8F", "#3E8E5E"],
    notes: "Keep the stock hot and ladle slowly. The final beat of butter and cheese off the heat is the mantecatura.",
    ingredients: [
      ing("Arborio rice", "320 g", "INV-0052"),
      ing("Chicken stock, hot", "1.2 L", "INV-0060"),
      ing("Yellow onion, finely diced", "1", "INV-0054"),
      ing("Dry white wine", "120 ml"),
      ing("Parmesan, grated", "70 g", "INV-0050"),
      ing("Unsalted butter", "40 g", "INV-0048"),
      ing("Lemon", "1", "INV-0057"),
    ],
    steps: [
      step("Sweat the onion in a little butter until translucent. Add the rice and toast for a minute."),
      step("Pour in the wine and let it cook away."),
      step("Add hot stock a ladle at a time, stirring, waiting until each is absorbed before the next — about 18 minutes."),
      step("Off the heat, beat in the cold butter, parmesan and lemon zest. Loosen with stock and rest 2 minutes before serving."),
    ],
  },
  {
    id: "rcp-chocmousse", name: "Dark chocolate mousse", category: "Dessert", difficulty: "Medium",
    servings: 6, prep: 25, cook: 0, hasImage: true, visibility: "Private",
    owner: "Nora Quintana", created: "18 May 2026", updated: "3 weeks ago",
    image: ["#9A5B47", "#4A2A22"],
    notes: "Fold gently to keep the air in. Set overnight for the best texture.",
    ingredients: [
      ing("Dark chocolate 70%", "200 g", "INV-0058"),
      ing("Large eggs, separated", "4", "INV-0047"),
      ing("Caster sugar", "40 g", "INV-0046"),
      ing("Double cream", "150 ml", "INV-0061"),
      ing("Pinch of sea salt", null, "INV-0043"),
    ],
    steps: [
      step("Melt the chocolate gently and let it cool to just warm."),
      step("Whisk the yolks with half the sugar, then fold into the chocolate."),
      step("Whip the cream to soft peaks; fold through."),
      step("Whisk the whites with the remaining sugar and a pinch of salt to soft peaks, then fold in carefully in three additions."),
      step("Divide into glasses and chill for at least 4 hours, ideally overnight."),
    ],
  },
  {
    id: "rcp-pancakes", name: "Buttermilk pancakes", category: "Breakfast", difficulty: "Easy",
    servings: 4, prep: 10, cook: 15, hasImage: true, visibility: "Public",
    owner: "Diego Salas", created: "27 Feb 2026", updated: "5 days ago",
    image: ["#F0C75E", "#C98A2E"],
    notes: "Don't overmix — lumps are fine. Rest the batter while the pan heats.",
    ingredients: [
      ing("Plain flour", "240 g", "INV-0045"),
      ing("Large eggs", "2", "INV-0047"),
      ing("Whole milk", "300 ml", "INV-0049"),
      ing("Unsalted butter, melted", "40 g", "INV-0048"),
      ing("Caster sugar", "2 tbsp", "INV-0046"),
      ing("Baking powder", "2 tsp"),
      ing("Honey, to serve", null, "INV-0065"),
    ],
    steps: [
      step("Whisk the dry ingredients in one bowl, the wet in another."),
      step("Combine with a few strokes until just mixed; rest 10 minutes."),
      step("Cook spoonfuls on a buttered pan over medium heat until bubbles form, then flip. Serve with honey."),
    ],
  },
  {
    id: "rcp-hummus", name: "Whipped hummus", category: "Starter", difficulty: "Easy",
    servings: 6, prep: 15, cook: 0, hasImage: false, visibility: "Public",
    owner: "Hugo Belmonte", created: "9 Apr 2026", updated: "2 weeks ago",
    notes: "Peeling the chickpeas is tedious but gives a much smoother result.",
    ingredients: [
      ing("Chickpeas", "2 tins", "INV-0063"),
      ing("Tahini", "3 tbsp"),
      ing("Lemon, juiced", "1", "INV-0057"),
      ing("Garlic clove", "1", "INV-0053"),
      ing("Extra-virgin olive oil", "60 ml", "INV-0042"),
      ing("Smoked paprika, to finish", null, "INV-0062"),
    ],
    steps: [
      step("Blend the chickpeas with tahini, lemon and garlic until smooth."),
      step("Stream in iced water until light and whipped; season."),
      step("Spread, pool with olive oil and dust with smoked paprika."),
    ],
  },
  {
    id: "rcp-sangria", name: "Summer sangria", category: "Drink", difficulty: "Easy",
    servings: 8, prep: 15, cook: 0, hasImage: true, visibility: "Public",
    owner: "Marina Velasco", created: "1 Jun 2026", updated: "3 days ago",
    image: ["#C85A8A", "#7A2A5A"],
    notes: "Macerate the fruit for a few hours before serving. Top with soda just before pouring.",
    ingredients: [
      ing("Red wine (Rioja)", "1 bottle"),
      ing("Orange, sliced", "1"),
      ing("Lemon, sliced", "1", "INV-0057"),
      ing("Brandy", "60 ml"),
      ing("Caster sugar", "2 tbsp", "INV-0046"),
      ing("Soda water, to top", null),
    ],
    steps: [
      step("Combine the wine, brandy, sugar and fruit in a jug."),
      step("Chill for at least 3 hours to macerate."),
      step("Serve over ice and top each glass with a splash of soda."),
    ],
  },
  {
    id: "rcp-tomatosauce", name: "Slow tomato sauce", category: "Sauce", difficulty: "Easy",
    servings: 6, prep: 5, cook: 45, hasImage: false, visibility: "Public",
    owner: "Diego Salas", created: "11 Jan 2026", updated: "1 month ago",
    notes: "Freezes beautifully. A whole peeled clove and a basil sprig keep it fragrant.",
    ingredients: [
      ing("Tinned plum tomatoes", "2 tins", "INV-0055"),
      ing("Garlic cloves", "2", "INV-0053"),
      ing("Extra-virgin olive oil", "4 tbsp", "INV-0042"),
      ing("Fresh basil", "a few leaves", "INV-0056"),
      ing("Fine sea salt", "to taste", "INV-0043"),
    ],
    steps: [
      step("Warm the oil and gently colour the garlic."),
      step("Add the tomatoes, crushing by hand, and a pinch of salt."),
      step("Simmer low and slow for 40 minutes, then stir through torn basil."),
    ],
  },
  {
    id: "rcp-crema", name: "Crema catalana", category: "Dessert", difficulty: "Hard",
    servings: 6, prep: 25, cook: 15, hasImage: true, visibility: "Public",
    owner: "Nora Quintana", created: "22 Nov 2025", updated: "2 months ago",
    image: ["#EAB94E", "#A56A1E"],
    notes: "Infuse the milk with citrus zest and cinnamon. Caramelise the sugar just before serving.",
    ingredients: [
      ing("Whole milk", "500 ml", "INV-0049"),
      ing("Egg yolks", "6", "INV-0047"),
      ing("Caster sugar", "120 g", "INV-0046"),
      ing("Cornflour", "30 g"),
      ing("Lemon zest", "1 strip", "INV-0057"),
      ing("Cinnamon stick", "1"),
    ],
    steps: [
      step("Infuse the milk with the zest and cinnamon over low heat; let stand."),
      step("Whisk the yolks, sugar and cornflour to a pale paste."),
      step("Temper with the warm milk, then cook gently until thickened."),
      step("Pour into shallow dishes, chill, then caramelise a thin layer of sugar on top."),
    ],
  },
  {
    id: "rcp-yoghurtbowl", name: "Honey yoghurt bowl", category: "Breakfast", difficulty: "Easy",
    servings: 2, prep: 8, cook: 0, hasImage: false, visibility: "Public",
    owner: "Hugo Belmonte", created: "12 Apr 2026", updated: "1 week ago",
    notes: "A fast, no-cook breakfast. Vary the fruit with the season.",
    ingredients: [
      ing("Greek yoghurt", "400 g", "INV-0064"),
      ing("Honey", "2 tbsp", "INV-0065"),
      ing("Mixed berries", "150 g"),
      ing("Toasted almonds", "a handful"),
    ],
    steps: [
      step("Spoon the yoghurt into bowls."),
      step("Top with berries and almonds, then drizzle with honey."),
    ],
  },
  {
    id: "rcp-aioli", name: "Garlic aioli", category: "Sauce", difficulty: "Medium",
    servings: 8, prep: 15, cook: 0, hasImage: false, visibility: "Private",
    owner: "Marina Velasco", created: "8 Mar 2026", updated: "3 weeks ago",
    notes: "Add the oil drop by drop at first or it will split. Room-temperature egg helps.",
    ingredients: [
      ing("Garlic cloves", "2", "INV-0053"),
      ing("Egg yolk", "1", "INV-0047"),
      ing("Extra-virgin olive oil", "200 ml", "INV-0042"),
      ing("Lemon juice", "1 tsp", "INV-0057"),
      ing("Fine sea salt", "a pinch", "INV-0043"),
    ],
    steps: [
      step("Pound the garlic with salt to a paste."),
      step("Whisk in the yolk, then add the oil drop by drop, building an emulsion."),
      step("Loosen with lemon juice and season to taste."),
    ],
  },
  {
    id: "rcp-paella", name: "Seafood paella", category: "Main", difficulty: "Hard",
    servings: 6, prep: 30, cook: 35, hasImage: true, visibility: "Public",
    owner: "Diego Salas", created: "19 Feb 2026", updated: "6 days ago",
    image: ["#E2A23E", "#B5471F"],
    notes: "Don't stir once the rice is in — you want the socarrat to form on the base.",
    ingredients: [
      ing("Bomba rice", "400 g"),
      ing("Fish stock, hot", "1 L"),
      ing("Mixed seafood", "600 g"),
      ing("Tinned plum tomatoes", "1 tin", "INV-0055"),
      ing("Garlic cloves", "3", "INV-0053"),
      ing("Smoked paprika", "1 tsp", "INV-0062"),
      ing("Saffron", "a pinch"),
      ing("Extra-virgin olive oil", "4 tbsp", "INV-0042"),
    ],
    steps: [
      step("Build a sofrito with garlic, tomato and paprika in the paella pan."),
      step("Stir in the rice to coat, then add the saffron-infused hot stock and spread evenly."),
      step("Simmer without stirring until the rice is nearly done, then nestle in the seafood."),
      step("Finish over higher heat to form the socarrat, then rest, covered, for 5 minutes."),
    ],
  },
  {
    id: "rcp-lemonade", name: "Cloudy lemonade", category: "Drink", difficulty: "Easy",
    servings: 6, prep: 10, cook: 0, hasImage: false, visibility: "Public",
    owner: "Nora Quintana", created: "5 Jun 2026", updated: "4 days ago",
    notes: "A simple syrup dissolves far better than loose sugar. Keep it tart.",
    ingredients: [
      ing("Lemons, juiced", "6", "INV-0057"),
      ing("Caster sugar", "120 g", "INV-0046"),
      ing("Cold water", "1 L"),
      ing("Mint sprigs", "to serve"),
    ],
    steps: [
      step("Make a simple syrup with the sugar and a little hot water; cool."),
      step("Combine with the lemon juice and cold water; taste and adjust."),
      step("Serve over ice with plenty of mint."),
    ],
  },
];
const RCP = Object.fromEntries(RECIPES.map((r) => [r.id, r]));

// ── Weekly menu (ISO week beginning Monday 22 Jun 2026) ──────────
// grid[dayIndex 0–6][slotKey] = array of recipe ids. Empty slots are simply
// absent. Slots reference recipes only — no free text.
const WEEK_MONDAY = "2026-06-22";
const MENU = {
  id: "menu-45", week: WEEK_MONDAY, name: "This week", visibility: "Public",
  owner: "Marina Velasco", created: "20 Jun 2026", updated: "Today",
  grid: {
    0: { Breakfast: ["rcp-pancakes"], Lunch: ["rcp-tortilla"], Dinner: ["rcp-carbonara"] },
    1: { Breakfast: ["rcp-yoghurtbowl"], Lunch: ["rcp-gazpacho", "rcp-hummus"], Dinner: ["rcp-risotto"] },
    2: { Lunch: ["rcp-paella"], Snack: ["rcp-lemonade"], Dinner: ["rcp-gazpacho"] },
    3: { Breakfast: ["rcp-pancakes"], Dinner: ["rcp-tortilla"], Snack: ["rcp-yoghurtbowl"] },
    4: { Lunch: ["rcp-carbonara"], Dinner: ["rcp-paella"], Snack: ["rcp-chocmousse"] },
    5: { Breakfast: ["rcp-yoghurtbowl"], Lunch: ["rcp-hummus", "rcp-gazpacho"], Dinner: ["rcp-risotto"], Snack: ["rcp-sangria"] },
    6: { Breakfast: ["rcp-pancakes"], Lunch: ["rcp-tortilla"], Dinner: ["rcp-crema"] },
  },
};

// ── Formatting helpers ──────────────────────────────────────────
function fmtMins(m) {
  if (m == null) return null;
  if (m === 0) return "0 min";
  if (m < 60) return `${m} min`;
  const h = Math.floor(m / 60), r = m % 60;
  return r ? `${h} h ${r} min` : `${h} h`;
}
function totalTime(r) {
  const p = r.prep || 0, c = r.cook || 0;
  return (r.prep != null || r.cook != null) ? p + c : null;
}
function parseISO(s) { const [y, m, d] = s.split("-").map(Number); return new Date(y, m - 1, d); }
function isoOf(date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
}
// Monday of the ISO week containing `date`.
function mondayOf(date) {
  const d = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  const dow = (d.getDay() + 6) % 7; // 0 = Monday
  d.setDate(d.getDate() - dow);
  return d;
}
function addDays(date, n) { const d = new Date(date); d.setDate(d.getDate() + n); return d; }
// Seven Date objects Monday→Sunday for the week beginning at `mondayISO`.
function weekDays(mondayISO) {
  const mon = parseISO(mondayISO);
  return Array.from({ length: 7 }, (_, i) => addDays(mon, i));
}
function fmtWeekRange(mondayISO) {
  const days = weekDays(mondayISO);
  const a = days[0], b = days[6];
  const sameMonth = a.getMonth() === b.getMonth();
  return sameMonth
    ? `${a.getDate()}–${b.getDate()} ${MONTHS_SHORT[b.getMonth()]} ${b.getFullYear()}`
    : `${a.getDate()} ${MONTHS_SHORT[a.getMonth()]} – ${b.getDate()} ${MONTHS_SHORT[b.getMonth()]} ${b.getFullYear()}`;
}

window.SEG_RECIPES = RECIPES;
window.SEG_INVENTORY = INVENTORY;
window.SEG_MENU = MENU;
window.SegRec = {
  TODAY, MONTHS, MONTHS_SHORT, DOW_SHORT, DOW_LONG, WEEK_MONDAY,
  REC_CATEGORIES, CAT, REC_DIFFICULTY, DIFFICULTY_ORDER, MEAL_SLOTS,
  RCP, INV,
  fmtMins, totalTime, parseISO, isoOf, mondayOf, addDays, weekDays, fmtWeekRange,
};
})();
