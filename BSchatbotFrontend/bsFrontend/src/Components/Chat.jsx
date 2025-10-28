import { useEffect, useState, useRef } from 'react'
import '../App.css'
import '../../node_modules/normalize.css/normalize.css'
import remarkGfm from 'remark-gfm';
import ReactMarkdown from 'react-markdown';

function Chat({children}){
     
        const [messages, setMessages] = useState([]);
        const [input, setInput] = useState('');
        const[conversationId, setConversationId] = useState(1);
        const [conversations, setConversations] = useState([]);
        const messagesEndRef = useRef(null);

        useEffect(() => {
            scrollToBottom();
        }, [messages]);

        useEffect(() => {
            const loadConversations = async () => {
                const response = await fetch(`https://localhost:7127/api/Chat/conversations`, {
                    headers:{
                        "Content-Type": "application/json",
                    },
                    credentials: 'include',
                });


                if(!response.ok){
                console.error('Failed to fetch conversations');
                return;
                }

                const data = await response.json();
                setConversations(data);

                if (data.length > 0) {
                    setConversationId(data[0].id);
                    setMessages(data[0].messages);
                }
                console.log(data);

            }

            loadConversations();
            }, []);


        const scrollToBottom = () => {
        if (messagesEndRef.current) {
            messagesEndRef.current.scrollIntoView({ behavior: "smooth" });
        }
        };


        const startNewConversation = async () => {
            const response = await fetch('https://localhost:7127/api/chat/addConversation',{
                method: 'POST',
                headers:{
                "Content-Type": "application/json"
                },
                credentials: 'include',
            })

            if(!response.ok){
                console.error('Failed to create new conversation');
                return;
            }

            const newConv = await response.json();
            setConversations(prev => [...prev, newConv]);
            setConversationId(newConv.id);
            setMessages(newConv.messages || []);
        };

        const updateConversationMessages = (convId, newMessages) => {
            setConversations(prev =>
                prev.map(c =>
                c.id === convId ? { ...c, messages: newMessages } : c
                )
            );
        };

        const fetchData = async () => {
            if(!input.trim()) {
                return;
            }

            setMessages(prev => {
                const updated = [...prev, { role: 'user', text: input }];
                updateConversationMessages(conversationId, updated);
                return updated;
            });
            setInput('');



            const response = await fetch(`https://localhost:7127/api/chat`, {
                method: 'POST',
                headers: {
                'Content-Type': 'application/json',
                },
                body: JSON.stringify({ prompt: input, conversationId: conversationId }),
                credentials: 'include'
            })

            if(!response.ok || !response.body) {
                const message = `An error has occured: ${response.status}`;
                throw new Error(message);
            }

            const reader = response.body.getReader();

            let buffer = '';
            let assistantText = '';

            setMessages(prev => {
                const updated = [...prev, { role: 'assistant', text: '' }];
                updateConversationMessages(conversationId, updated);
                return updated;
            });

            while (true) {
                const { done, value } = await reader.read();

                if (done) {
                    break;
                }

                if(!value){
                    return;
                }

                buffer += new TextDecoder().decode(value);

                // Process complete JSON messages separated by <|>
                let boundaryIndex;
                while ((boundaryIndex = buffer.indexOf('<|>')) !== -1) {
                    const chunk = buffer.slice(0, boundaryIndex);
                    buffer = buffer.slice(boundaryIndex + 3);
                    if (!chunk) continue;

                    assistantText += chunk;
                    setMessages(prev => {
                        const updated = [...prev];
                        const lastIndex = updated.length - 1;
                        if (updated[lastIndex]?.role === 'assistant') {
                            updated[lastIndex] = { ...updated[lastIndex], text: assistantText };
                        }
                        updateConversationMessages(conversationId, updated);
                            return updated;
                    });
                }

            }
        }


        const handleKeyDown = (e) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            fetchData();
        }
        };


    return (
    <div className='App'>
        <aside className='sidemenu'>
            <div className='side-menu-button' onClick={startNewConversation}>
                <span>
                +
                </span>
                New chat  
            </div>
            <div className="side-menu-chats-history">
                {conversations.map(conv => (
                <div
                key={conv.id}
                className={`side-menu-item ${conv.id === conversationId ? 'active' : ''}`}
                onClick={() => {
                    setConversationId(conv.id);
                    setMessages(conv.messages);
                }}
                >
                {conv.title}
                </div>))}
            </div>
            <div className="logout-section">
                {children}
            </div>
        </aside>
        <section className='chatbox'>
        <div className='chat-log'>
            {messages.map((msg, i) => (
            <div key={i} className={msg.role === 'assistant' ? 'chat-kimi' : 'chat-message'}>
                <div className="chat-message-center">
                <div className={`avatar ${msg.role === 'assistant' ? 'chatkimi' : ''}`}>
                    {msg.role === 'assistant' 
                    ? <img src="https://unpkg.com/@lobehub/icons-static-svg@latest/icons/kimi-color.svg" />  
                    : 'me'}
                </div>
                <div className="message" ref={messagesEndRef}>
                    <ReactMarkdown remarkPlugins={[remarkGfm]}>
                    {msg.text}
                    </ReactMarkdown>
                </div>
                </div>
            </div>
            ))}        
        </div>

        <div className="chat-input-holder">
            <textarea 
            rows={1}
            className='chat-input-textarea' 
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder='Type a message...'>

            </textarea>
        </div>
        </section>
    </div>
    );
}

export default Chat;