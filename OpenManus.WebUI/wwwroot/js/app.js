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
    // 重置高度以获取正确的 scrollHeight
    element.style.height = 'auto';
    
    // 计算新高度，设置最小高度为一行，最大高度为6行
    const minHeight = 48; // 约一行的高度
    const maxHeight = 144; // 约6行的高度
    const newHeight = Math.min(Math.max(element.scrollHeight, minHeight), maxHeight);
    
    element.style.height = newHeight + 'px';
    
    // 如果内容超过最大高度，显示滚动条
    if (element.scrollHeight > maxHeight) {
        element.style.overflowY = 'auto';
    } else {
        element.style.overflowY = 'hidden';
    }
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

// 更新执行停靠区域位置
window.updateExecutionDockPosition = (executionDockElement) => {
    if (!executionDockElement) return;
    
    // 查找输入框区域
    const inputArea = document.querySelector('.agent-input-area');
    if (!inputArea) {
        // 如果找不到输入框，使用默认位置
        executionDockElement.style.bottom = '120px';
        return;
    }
    
    // 获取输入框的实际高度
    const inputAreaHeight = inputArea.offsetHeight;
    
    // 设置执行组件的bottom位置，留出一些间距
    const bottomOffset = inputAreaHeight + 10; // 10px间距
    executionDockElement.style.bottom = bottomOffset + 'px';
    
    // 监听窗口大小变化和输入框高度变化
    const resizeObserver = new ResizeObserver(() => {
        const newInputAreaHeight = inputArea.offsetHeight;
        const newBottomOffset = newInputAreaHeight + 10;
        executionDockElement.style.bottom = newBottomOffset + 'px';
    });
    
    // 开始观察输入框区域的大小变化
    resizeObserver.observe(inputArea);
    
    // 将观察器存储到元素上，以便后续清理
    executionDockElement._resizeObserver = resizeObserver;
};

// 清理执行停靠区域的观察器
window.cleanupExecutionDockObserver = (executionDockElement) => {
    if (executionDockElement && executionDockElement._resizeObserver) {
        executionDockElement._resizeObserver.disconnect();
        delete executionDockElement._resizeObserver;
    }
};

// 执行状态持久化管理
window.executionPersistence = {
    // 保存执行状态到localStorage
    saveExecutionState: (sessionId, executionResultJson) => {
        try {
            const key = `execution_state_${sessionId}`;
            const data = {
                executionResult: executionResultJson,
                timestamp: Date.now()
            };
            localStorage.setItem(key, JSON.stringify(data));
        } catch (error) {
            console.warn('保存执行状态失败:', error);
        }
    },
    
    // 从localStorage加载执行状态
    loadExecutionState: (sessionId) => {
        try {
            const key = `execution_state_${sessionId}`;
            const data = localStorage.getItem(key);
            if (data) {
                const parsed = JSON.parse(data);
                // 检查数据是否过期（24小时）
                const maxAge = 24 * 60 * 60 * 1000; // 24小时
                if (Date.now() - parsed.timestamp < maxAge) {
                    return parsed.executionResult;
                } else {
                    // 清除过期数据
                    localStorage.removeItem(key);
                }
            }
        } catch (error) {
            console.warn('加载执行状态失败:', error);
        }
        return null;
    },
    
    // 清除执行状态
    clearExecutionState: (sessionId) => {
        try {
            const key = `execution_state_${sessionId}`;
            localStorage.removeItem(key);
        } catch (error) {
            console.warn('清除执行状态失败:', error);
        }
    },
    
    // 清除所有过期的执行状态
    cleanupExpiredStates: () => {
        try {
            const maxAge = 24 * 60 * 60 * 1000; // 24小时
            const keysToRemove = [];
            
            for (let i = 0; i < localStorage.length; i++) {
                const key = localStorage.key(i);
                if (key && key.startsWith('execution_state_')) {
                    const data = localStorage.getItem(key);
                    if (data) {
                        try {
                            const parsed = JSON.parse(data);
                            if (Date.now() - parsed.timestamp >= maxAge) {
                                keysToRemove.push(key);
                            }
                        } catch (e) {
                            keysToRemove.push(key);
                        }
                    }
                }
            }
            
            keysToRemove.forEach(key => localStorage.removeItem(key));
        } catch (error) {
            console.warn('清理过期执行状态失败:', error);
        }
    }
};

// 页面加载时清理过期状态
document.addEventListener('DOMContentLoaded', () => {
    window.executionPersistence.cleanupExpiredStates();
});

// 平滑滚动到底部
window.smoothScrollToBottom = (elementId) => {
    const element = document.getElementById(elementId);
    if (element) {
        // 如果是消息容器，滚动其父容器
        const container = element.closest('.messages-container');
        if (container) {
            container.scrollTo({
                top: container.scrollHeight,
                behavior: 'smooth'
            });
        } else {
            element.scrollTo({
                top: element.scrollHeight,
                behavior: 'smooth'
            });
        }
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

