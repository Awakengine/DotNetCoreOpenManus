// OpenManus WebUI JavaScript 支持

window.scrollToBottom = (elementId) => {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};

window.downloadFile = (filename, content) => {
    const element = document.createElement('a');
    element.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(content));
    element.setAttribute('download', filename);
    element.style.display = 'none';
    document.body.appendChild(element);
    element.click();
    document.body.removeChild(element);
};

window.copyToClipboard = (text) => {
    navigator.clipboard.writeText(text).then(() => {
        console.log('Text copied to clipboard');
    }).catch(err => {
        console.error('Failed to copy text: ', err);
    });
};

// 文件预览功能
window.previewFile = (filePath, fileType) => {
    // 根据文件类型处理预览逻辑
    console.log('Previewing file:', filePath, 'Type:', fileType);
};

// 键盘快捷键支持
document.addEventListener('keydown', (event) => {
    // Ctrl+Enter 发送消息
    if (event.ctrlKey && event.key === 'Enter') {
        const sendButton = document.querySelector('.chat-input-area .btn-primary');
        if (sendButton && !sendButton.disabled) {
            sendButton.click();
        }
    }
});

// 自动调整文本区域高度
window.autoResizeTextarea = (element) => {
    element.style.height = 'auto';
    element.style.height = element.scrollHeight + 'px';
};

// 现代化UI交互效果
window.initModernUI = () => {
    // 添加页面加载动画
    document.body.style.opacity = '0';
    document.body.style.transition = 'opacity 0.5s ease';
    
    setTimeout(() => {
        document.body.style.opacity = '1';
    }, 100);
    
    // 工具项悬停效果增强
    const toolItems = document.querySelectorAll('.tool-item');
    toolItems.forEach(item => {
        item.addEventListener('mouseenter', () => {
            item.style.transform = 'translateY(-4px) scale(1.02)';
        });
        
        item.addEventListener('mouseleave', () => {
            item.style.transform = 'translateY(0) scale(1)';
        });
    });
    
    // 消息输入框焦点效果
    const inputField = document.querySelector('.agent-input-area .form-control');
    if (inputField) {
        inputField.addEventListener('focus', () => {
            inputField.parentElement.style.transform = 'scale(1.02)';
        });
        
        inputField.addEventListener('blur', () => {
            inputField.parentElement.style.transform = 'scale(1)';
        });
    }
};

// 平滑滚动到底部
window.smoothScrollToBottom = (elementId) => {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollTo({
            top: element.scrollHeight,
            behavior: 'smooth'
        });
    }
};

// 打字机效果
window.typewriterEffect = (elementId, text, speed = 50) => {
    const element = document.getElementById(elementId);
    if (!element) return;
    
    element.innerHTML = '';
    let i = 0;
    
    const typeWriter = () => {
        if (i < text.length) {
            element.innerHTML += text.charAt(i);
            i++;
            setTimeout(typeWriter, speed);
        }
    };
    
    typeWriter();
};

// 页面加载完成后初始化
document.addEventListener('DOMContentLoaded', () => {
    window.initModernUI();
});

// Blazor页面更新后重新初始化
window.addEventListener('blazor:navigated', () => {
    setTimeout(() => {
        window.initModernUI();
    }, 100);
});

