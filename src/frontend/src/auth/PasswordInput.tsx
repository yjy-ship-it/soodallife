import { useState } from 'react'
import './passwordInput.css'

type PasswordInputProps = {
  value: string
  onChange: (value: string) => void
  autoComplete?: 'current-password' | 'new-password'
  ariaLabel?: string
}

export function PasswordInput({ value, onChange, autoComplete = 'new-password', ariaLabel = '비밀번호' }: PasswordInputProps) {
  const [visible, setVisible] = useState(false)
  const actionLabel = visible ? '비밀번호 숨기기' : '비밀번호 보기'

  return <div className="passwordInputField">
    <input
      type={visible ? 'text' : 'password'}
      minLength={6}
      maxLength={128}
      value={value}
      onChange={event => onChange(event.target.value)}
      autoComplete={autoComplete}
      aria-label={ariaLabel}
    />
    <button type="button" className="passwordVisibilityButton" aria-label={actionLabel} aria-pressed={visible} onClick={() => setVisible(current => !current)}>
      {visible ? '숨기기' : '보기'}
    </button>
  </div>
}
