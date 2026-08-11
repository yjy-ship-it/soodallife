export type NativeFile = { name: string; type: string; size: number; uri: string }
export type Coordinates = { latitude: number; longitude: number; accuracy: number }

export interface NativeCapabilities {
  camera?: { capture(): Promise<NativeFile> }
  filePicker?: { pick(accept?: string[]): Promise<NativeFile[]> }
  geolocation?: { current(): Promise<Coordinates> }
  notifications?: { requestPermission(): Promise<boolean> }
  deepLinks?: { subscribe(handler: (url: string) => void): () => void }
  secureStorage?: { get(key: string): Promise<string | null>; set(key: string, value: string): Promise<void>; remove(key: string): Promise<void> }
  appLifecycle?: { subscribe(handler: (active: boolean) => void): () => void }
}

// Capacitor 등 네이티브 브리지는 이 인터페이스를 구현해 주입한다.
// 웹 업무 코드는 네이티브 SDK를 직접 참조하지 않는다.
export const webNativeCapabilities: NativeCapabilities = {}
