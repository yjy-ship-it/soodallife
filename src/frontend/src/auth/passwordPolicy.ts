export const accountPasswordGuidance = '6~128자이며 영문자, 숫자, 특수문자를 각각 1자 이상 포함해 주세요.'

const isAsciiLetter = (code: number) => code >= 65 && code <= 90 || code >= 97 && code <= 122
const isAsciiDigit = (code: number) => code >= 48 && code <= 57
const isAsciiSpecial = (code: number) => code >= 33 && code <= 47 || code >= 58 && code <= 64 || code >= 91 && code <= 96 || code >= 123 && code <= 126

export function isAccountPasswordValid(value: string) {
  if (value.length < 6 || value.length > 128 || [...value].some(character => /\s/u.test(character))) return false
  const codes = [...value].map(character => character.charCodeAt(0))
  return codes.some(isAsciiLetter) && codes.some(isAsciiDigit) && codes.some(isAsciiSpecial)
}
