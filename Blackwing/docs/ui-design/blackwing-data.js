// Blackwing — sample data module (business logic / default data only, no UI).
// Deterministic pseudo-random generation so the demo dataset is stable across reloads.

function mulberry32(seed) {
  return function () {
    seed |= 0; seed = (seed + 0x6D2B79F5) | 0;
    let t = Math.imul(seed ^ (seed >>> 15), 1 | seed);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

export const PEOPLE = ["Mia", "Théo", "Priya", "Grandpa Sam", "Noah", "Iris", "Marcus", "Zoe", "Dad", "Elena"];
export const PLACES = ["Lisbon", "Porto waterfront", "Grandma's kitchen", "Blue Ridge trail", "Big Sur", "Home studio", "Oaxaca market", "The lake house", "Amalfi coast", "Kyoto station"];
export const TOPICS = ["Family dinner", "Golden hour", "Street photography", "Film scans", "Architecture", "Birthday", "Road trip", "Wildlife", "Grain test roll", "Sunset"];

const HUE_ANCHORS = [176, 40, 204, 9, 152, 28, 190, 340, 60, 260];

function pickN(rand, pool, n) {
  const copy = pool.slice();
  const out = [];
  for (let i = 0; i < n && copy.length; i++) {
    const idx = Math.floor(rand() * copy.length);
    out.push(copy[idx]);
    copy.splice(idx, 1);
  }
  return out;
}

function weightedCount(rand, weights) {
  // weights: array of probabilities summing to 1, index = count
  const r = rand();
  let acc = 0;
  for (let i = 0; i < weights.length; i++) {
    acc += weights[i];
    if (r <= acc) return i;
  }
  return weights.length - 1;
}

function gradientFor(hue) {
  const hue2 = (hue + 34) % 360;
  return `linear-gradient(135deg, hsl(${hue} 58% 80%), hsl(${hue2} 55% 54%))`;
}

function buildImages() {
  const rand = mulberry32(20260710);
  const now = new Date("2026-07-10T09:00:00");
  const total = 120;
  const pendingCount = 12;
  const images = [];

  for (let i = 0; i < total; i++) {
    const isPending = i < pendingCount;
    const hue = (HUE_ANCHORS[i % HUE_ANCHORS.length] + Math.floor(rand() * 20) - 10 + 360) % 360;
    const orientationRoll = rand();
    let width, height;
    if (orientationRoll < 0.45) { width = 4032; height = 3024; }
    else if (orientationRoll < 0.85) { width = 3024; height = 4032; }
    else { width = 3400; height = 3400; }

    const daysAgo = isPending ? rand() * 5 : 5 + rand() * 725;
    const captureDate = new Date(now.getTime() - daysAgo * 86400000);

    let tags = { person: [], place: [], topic: [] };
    if (!isPending) {
      const nPerson = weightedCount(rand, [0.4, 0.4, 0.2]);
      const nPlace = weightedCount(rand, [0.3, 0.7]);
      const nTopic = weightedCount(rand, [0.3, 0.5, 0.2]);
      tags = {
        person: pickN(rand, PEOPLE, nPerson),
        place: pickN(rand, PLACES, nPlace),
        topic: pickN(rand, TOPICS, nTopic),
      };
    }

    images.push({
      id: "img-" + (i + 1),
      hue,
      grad: gradientFor(hue),
      captureDate: captureDate.toISOString(),
      width,
      height,
      sizeKB: Math.round(1800 + rand() * 4400),
      pending: isPending,
      tags,
    });
  }
  return images;
}

export const IMAGES = buildImages();

export const USERS = [
  { id: "u1", username: "jordan.reyes", role: "user", created: "2025-02-11", lastActive: "2026-07-10T08:40:00" },
  { id: "u2", username: "priya.natarajan", role: "user", created: "2025-04-03", lastActive: "2026-07-09T21:12:00" },
  { id: "u3", username: "sam.okafor", role: "user", created: "2025-06-22", lastActive: "2026-07-08T14:05:00" },
  { id: "u4", username: "elena.vas", role: "user", created: "2025-09-14", lastActive: "2026-06-30T10:02:00" },
  { id: "u5", username: "admin.lina", role: "admin", created: "2024-11-01", lastActive: "2026-07-10T07:55:00" },
];
