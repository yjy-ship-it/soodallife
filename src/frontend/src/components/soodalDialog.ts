type DialogKind = 'alert' | 'confirm' | 'prompt'

type DialogOptions = {
  title?: string
  confirmText?: string
  cancelText?: string
  defaultValue?: string
  multiline?: boolean
}

let dialogQueue: Promise<unknown> = Promise.resolve()

function enqueue<T>(open: () => Promise<T>): Promise<T> {
  const next = dialogQueue.then(open, open)
  dialogQueue = next.then(() => undefined, () => undefined)
  return next
}

function showDialog(kind: DialogKind, message: string, options: DialogOptions = {}): Promise<string | boolean | null | void> {
  return enqueue(() => new Promise(resolve => {
    const active = document.activeElement instanceof HTMLElement ? document.activeElement : null
    const backdrop = document.createElement('div')
    backdrop.className = 'soodalDialogBackdrop'

    const dialog = document.createElement('section')
    dialog.className = 'soodalDialog'
    dialog.setAttribute('role', 'dialog')
    dialog.setAttribute('aria-modal', 'true')
    dialog.setAttribute('aria-labelledby', 'soodal-dialog-title')

    const eyebrow = document.createElement('span')
    eyebrow.className = 'soodalDialogEyebrow'
    eyebrow.textContent = 'SOODAL LIFE'

    const title = document.createElement('h2')
    title.id = 'soodal-dialog-title'
    title.textContent = options.title ?? (kind === 'prompt' ? '내용 입력' : kind === 'confirm' ? '확인해 주세요' : '안내')

    const description = document.createElement('p')
    description.className = 'soodalDialogMessage'
    description.textContent = message
    dialog.append(eyebrow, title, description)

    let input: HTMLInputElement | HTMLTextAreaElement | null = null
    if (kind === 'prompt') {
      input = options.multiline === false ? document.createElement('input') : document.createElement('textarea')
      input.className = 'soodalDialogInput'
      input.value = options.defaultValue ?? ''
      input.setAttribute('aria-label', message)
      if (input instanceof HTMLTextAreaElement) input.rows = 4
      dialog.append(input)
    }

    const actions = document.createElement('div')
    actions.className = 'soodalDialogActions'
    const close = (value: string | boolean | null | void) => {
      document.removeEventListener('keydown', onKeyDown)
      backdrop.remove()
      document.body.classList.remove('soodalDialogOpen')
      active?.focus()
      resolve(value)
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key !== 'Escape') return
      event.preventDefault()
      close(kind === 'prompt' ? null : kind === 'confirm' ? false : undefined)
    }

    if (kind !== 'alert') {
      const cancel = document.createElement('button')
      cancel.type = 'button'
      cancel.className = 'soodalDialogCancel'
      cancel.textContent = options.cancelText ?? '취소'
      cancel.addEventListener('click', () => close(kind === 'prompt' ? null : false))
      actions.append(cancel)
    }

    const confirm = document.createElement('button')
    confirm.type = 'button'
    confirm.className = 'soodalDialogConfirm'
    confirm.textContent = options.confirmText ?? '확인'
    confirm.addEventListener('click', () => {
      if (kind === 'prompt') close(input?.value ?? '')
      else if (kind === 'confirm') close(true)
      else close(undefined)
    })
    actions.append(confirm)
    dialog.append(actions)
    backdrop.append(dialog)
    document.body.append(backdrop)
    document.body.classList.add('soodalDialogOpen')
    document.addEventListener('keydown', onKeyDown)
    requestAnimationFrame(() => (input ?? confirm).focus())
  }))
}

export function soodalAlert(message: string, options?: DialogOptions): Promise<void> {
  return showDialog('alert', message, options) as Promise<void>
}

export function soodalConfirm(message: string, options?: DialogOptions): Promise<boolean> {
  return showDialog('confirm', message, options) as Promise<boolean>
}

export function soodalPrompt(message: string, defaultValue = '', options?: DialogOptions): Promise<string | null> {
  return showDialog('prompt', message, { ...options, defaultValue }) as Promise<string | null>
}
