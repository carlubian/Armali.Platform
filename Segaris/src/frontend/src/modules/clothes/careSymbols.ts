import dcAny from '@/assets/clothes-care/dc-any.png'
import dcNone from '@/assets/clothes-care/dc-none.png'
import drAny from '@/assets/clothes-care/dr-any.png'
import drDeli from '@/assets/clothes-care/dr-deli.png'
import drVeryDeli from '@/assets/clothes-care/dr-very-deli.png'
import irAny from '@/assets/clothes-care/ir-any.png'
import irLow from '@/assets/clothes-care/ir-low.png'
import irMed from '@/assets/clothes-care/ir-med.png'
import irNone from '@/assets/clothes-care/ir-none.png'
import wh30 from '@/assets/clothes-care/wh-30.png'
import wh30Deli from '@/assets/clothes-care/wh-30-deli.png'
import wh40 from '@/assets/clothes-care/wh-40.png'
import wh40Deli from '@/assets/clothes-care/wh-40-deli.png'
import wh50 from '@/assets/clothes-care/wh-50.png'
import wh50Deli from '@/assets/clothes-care/wh-50-deli.png'
import wh60 from '@/assets/clothes-care/wh-60.png'
import wh60Deli from '@/assets/clothes-care/wh-60-deli.png'
import whAny from '@/assets/clothes-care/wh-any.png'
import whHand from '@/assets/clothes-care/wh-hand.png'
import whNone from '@/assets/clothes-care/wh-none.png'
import type {
  ClothesDryCleaningCare,
  ClothesDryingCare,
  ClothesIroningCare,
  ClothesWashingCare,
} from '@/app/api/clothes'

export const washingCareSymbols: Record<ClothesWashingCare, string> = {
  Any: whAny,
  Wash30: wh30,
  Wash30Delicate: wh30Deli,
  Wash40: wh40,
  Wash40Delicate: wh40Deli,
  Wash50: wh50,
  Wash50Delicate: wh50Deli,
  Wash60: wh60,
  Wash60Delicate: wh60Deli,
  HandWash: whHand,
  DoNotWash: whNone,
}

export const dryingCareSymbols: Record<ClothesDryingCare, string> = {
  Any: drAny,
  Delicate: drDeli,
  VeryDelicate: drVeryDeli,
}

export const ironingCareSymbols: Record<ClothesIroningCare, string> = {
  Any: irAny,
  Low: irLow,
  Medium: irMed,
  DoNotIron: irNone,
}

export const dryCleaningCareSymbols: Record<ClothesDryCleaningCare, string> = {
  Any: dcAny,
  DoNotDryClean: dcNone,
}
