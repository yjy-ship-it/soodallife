import { useState } from 'react'
import './friendShare.css'
import { soodalPrompt } from './soodalDialog'

type FriendShareButtonProps = {
  title: string
  text: string
  url: string
  label?: string
  className?: string
}

function absoluteUrl(value: string) {
  return new URL(value, window.location.origin).toString()
}

export function FriendShareButton({ title, text, url, label = '친구에게 추천하기', className }: FriendShareButtonProps) {
  const [notice, setNotice] = useState('')

  const share = async () => {
    const target = absoluteUrl(url)
    setNotice('')
    try {
      if (navigator.share) {
        await navigator.share({ title, text, url: target })
        return
      }
      await navigator.clipboard.writeText(`${text}\n${target}`)
      setNotice('추천 링크를 복사했습니다. 카카오톡 대화창에 붙여 넣어 주세요.')
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') return
      try {
        await navigator.clipboard.writeText(`${text}\n${target}`)
        setNotice('추천 링크를 복사했습니다. 카카오톡 대화창에 붙여 넣어 주세요.')
      } catch {
        await soodalPrompt('아래 링크를 복사해 카카오톡으로 보내 주세요.', target)
      }
    }
  }

  return <span className="friendShare"><button className={className} type="button" onClick={() => void share()}><span aria-hidden="true">↗</span>{label}</button>{notice && <small role="status">{notice}</small>}</span>
}
