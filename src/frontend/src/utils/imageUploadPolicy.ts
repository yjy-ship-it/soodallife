const allowedTypes=new Set(['image/jpeg','image/png'])
const maximumBytes=5*1024*1024

export function installImageUploadPolicy(){
  const normalizeText=(root:Node)=>{
    const walker=document.createTreeWalker(root,NodeFilter.SHOW_TEXT)
    for(let node=walker.nextNode();node;node=walker.nextNode()){
      const parent=node.parentElement;if(!parent||['SCRIPT','STYLE'].includes(parent.tagName))continue
      node.textContent=(node.textContent??'')
        .replace(/PDF\/?JPG\/?PNG,?\s*최대\s*10MB/g,'JPG/JPEG/PNG, 최대 5MB')
        .replace(/JPG[·, ]+JPEG[·, ]+PNG[·, ]+PDF,?\s*파일당 최대\s*10MB/g,'JPG·JPEG·PNG, 파일당 최대 5MB')
        .replace(/사진[··]PDF/g,'사진(JPG·PNG, 최대 5MB)')
        .replace(/PDF, JPG, PNG/g,'JPG, JPEG, PNG')
    }
  }
  document.addEventListener('soodal-upload-policy-error',event=>{
    const message=(event as CustomEvent<{message:string}>).detail.message
    document.getElementById('soodal-upload-policy-notice')?.remove()
    const notice=document.createElement('section');notice.id='soodal-upload-policy-notice';notice.setAttribute('role','alert')
    Object.assign(notice.style,{position:'fixed',left:'50%',bottom:'28px',transform:'translateX(-50%)',zIndex:'2147483647',maxWidth:'min(520px,calc(100vw - 32px))',padding:'16px 20px',border:'1px solid #b9ddd8',borderRadius:'16px',background:'#fff',color:'#173c38',boxShadow:'0 14px 40px rgba(12,74,68,.22)',fontWeight:'700'})
    notice.textContent=message;document.body.appendChild(notice);window.setTimeout(()=>notice.remove(),4500)
  })
  const configure=(root:ParentNode)=>root.querySelectorAll<HTMLInputElement>('input[type="file"]').forEach(input=>{
    input.accept='.jpg,.jpeg,.png,image/jpeg,image/png'
    input.dataset.soodalImagePolicy='true'
  })
  configure(document)
  normalizeText(document.body)
  new MutationObserver(records=>records.forEach(record=>record.addedNodes.forEach(node=>{
    if(node instanceof Element){if(node.matches('input[type="file"]'))configure(node.parentElement??document);else configure(node);normalizeText(node)}
  }))).observe(document.documentElement,{childList:true,subtree:true})
  document.addEventListener('change',event=>{
    const input=event.target
    if(!(input instanceof HTMLInputElement)||input.type!=='file'||!input.files?.length)return
    const invalid=Array.from(input.files).find(file=>!allowedTypes.has(file.type)||file.size>maximumBytes)
    if(!invalid)return
    event.stopImmediatePropagation();input.value=''
    document.dispatchEvent(new CustomEvent('soodal-upload-policy-error',{detail:{message:!allowedTypes.has(invalid.type)?'JPG, JPEG, PNG 이미지만 업로드할 수 있습니다.':'이미지는 파일당 5MB 이하만 업로드할 수 있습니다.'}}))
  },true)
}
