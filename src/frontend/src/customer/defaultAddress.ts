import { customerAccountApi } from './accountApi'
import type { AdministrativeArea, CustomerAddress } from './accountTypes'

export const DEFAULT_ADDRESS_CHANGED_EVENT = 'soodal:default-address-changed'

export type DefaultAddressSelection = {
  address: CustomerAddress
  area: AdministrativeArea | null
  sidoId: string
  regionLabel: string
  fullAddress: string
}

export async function loadDefaultAddress(): Promise<DefaultAddressSelection | null> {
  const [addresses, areas] = await Promise.all([customerAccountApi.addresses(), customerAccountApi.sigungu()])
  const address = addresses.find(item => item.isDefault)
  if (!address) return null
  const area = areas.find(item => item.id === address.administrativeAreaId) ?? null
  return {
    address,
    area,
    sidoId: area?.parentId ?? '',
    regionLabel: area ? [area.parentName, area.name].filter(Boolean).join(' ') : address.administrativeAreaName ?? '',
    fullAddress: [address.roadAddress, address.detailAddress].filter(Boolean).join(' ').trim(),
  }
}

export function notifyDefaultAddressChanged() {
  window.dispatchEvent(new CustomEvent(DEFAULT_ADDRESS_CHANGED_EVENT))
}
