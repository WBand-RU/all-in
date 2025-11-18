import { useState, useEffect, useRef } from "react";
import {
    Send,
    Paperclip,
    Mic,
    MoreVertical,
    Edit2,
    Trash2,
    Reply,
    Smile,
    Users,
    Hash,
    Bell,
    BellOff,
} from "lucide-react";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import { Avatar, AvatarFallback, AvatarImage } from "@/shared/ui/avatar";
import { ScrollArea } from "@/shared/ui/scroll-area";
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from "@/shared/ui/card";
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from "@/shared/ui/dropdown-menu";
import { Badge } from "@/shared/ui/badge";
import { Textarea } from "@/shared/ui/textarea";
import { useToast } from "@/shared/ui/use-toast";
import { cn } from "@/lib/utils";

interface Message {
    id: string;
    senderId: string;
    senderName: string;
    senderAvatar?: string;
    content: string;
    timestamp: Date;
    type: "text" | "voice" | "file" | "system";
    edited?: boolean;
    replyTo?: Message;
    reactions?: { emoji: string; users: string[] }[];
    attachments?: { name: string; url: string; size: number }[];
}

interface ChatMember {
    id: string;
    name: string;
    avatar?: string;
    status: "online" | "away" | "offline";
    role: "owner" | "admin" | "member";
    isTyping?: boolean;
}

export function ChatPage() {
    const { toast } = useToast();
    const messagesEndRef = useRef<HTMLDivElement>(null);
    const fileInputRef = useRef<HTMLInputElement>(null);

    const [messages, setMessages] = useState<Message[]>([
        {
            id: "1",
            senderId: "system",
            senderName: "System",
            content:
                "Welcome to the band chat! This is where we coordinate and communicate.",
            timestamp: new Date(Date.now() - 86400000),
            type: "system",
        },
        {
            id: "2",
            senderId: "user1",
            senderName: "John Smith",
            senderAvatar: "/avatars/john.jpg",
            content: "Hey everyone! Ready for Sunday's service?",
            timestamp: new Date(Date.now() - 3600000),
            type: "text",
            reactions: [{ emoji: "👍", users: ["user2", "user3"] }],
        },
        {
            id: "3",
            senderId: "user2",
            senderName: "Sarah Johnson",
            senderAvatar: "/avatars/sarah.jpg",
            content:
                "Yes! I've been practicing the new arrangement of Amazing Grace.",
            timestamp: new Date(Date.now() - 3000000),
            type: "text",
        },
        {
            id: "4",
            senderId: "user3",
            senderName: "Mike Davis",
            senderAvatar: "/avatars/mike.jpg",
            content:
                "Can we go over the playlist one more time before rehearsal?",
            timestamp: new Date(Date.now() - 1800000),
            type: "text",
        },
        {
            id: "5",
            senderId: "currentUser",
            senderName: "You",
            content: "Sure! Let's meet 30 minutes early.",
            timestamp: new Date(Date.now() - 900000),
            type: "text",
            edited: true,
        },
    ]);

    const [members] = useState<ChatMember[]>([
        {
            id: "currentUser",
            name: "You",
            status: "online",
            role: "admin",
        },
        {
            id: "user1",
            name: "John Smith",
            avatar: "/avatars/john.jpg",
            status: "online",
            role: "owner",
            isTyping: false,
        },
        {
            id: "user2",
            name: "Sarah Johnson",
            avatar: "/avatars/sarah.jpg",
            status: "online",
            role: "member",
            isTyping: true,
        },
        {
            id: "user3",
            name: "Mike Davis",
            avatar: "/avatars/mike.jpg",
            status: "away",
            role: "member",
        },
        {
            id: "user4",
            name: "Emma Wilson",
            avatar: "/avatars/emma.jpg",
            status: "offline",
            role: "member",
        },
    ]);

    const [inputMessage, setInputMessage] = useState("");
    const [editingMessage, setEditingMessage] = useState<Message | null>(null);
    const [replyingTo, setReplyingTo] = useState<Message | null>(null);
    const [showEmojiPicker, setShowEmojiPicker] = useState(false);
    const [notificationsMuted, setNotificationsMuted] = useState(false);
    const currentUserId = "currentUser";

    useEffect(() => {
        scrollToBottom();
    }, [messages]);

    const scrollToBottom = () => {
        messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
    };

    const sendMessage = () => {
        if (!inputMessage.trim()) return;

        const newMessage: Message = {
            id: `msg-${Date.now()}`,
            senderId: currentUserId,
            senderName: "You",
            content: inputMessage,
            timestamp: new Date(),
            type: "text",
            replyTo: replyingTo || undefined,
        };

        if (editingMessage) {
            setMessages((prev) =>
                prev.map((msg) =>
                    msg.id === editingMessage.id
                        ? { ...msg, content: inputMessage, edited: true }
                        : msg,
                ),
            );
            setEditingMessage(null);
        } else {
            setMessages((prev) => [...prev, newMessage]);
        }

        setInputMessage("");
        setReplyingTo(null);
    };

    const deleteMessage = (messageId: string) => {
        setMessages((prev) => prev.filter((msg) => msg.id !== messageId));
        toast({
            title: "Message deleted",
            description: "The message has been removed from the chat.",
        });
    };

    const addReaction = (messageId: string, emoji: string) => {
        setMessages((prev) =>
            prev.map((msg) => {
                if (msg.id !== messageId) return msg;

                const existingReaction = msg.reactions?.find(
                    (r) => r.emoji === emoji,
                );
                if (existingReaction) {
                    if (existingReaction.users.includes(currentUserId)) {
                        // Remove reaction
                        existingReaction.users = existingReaction.users.filter(
                            (u) => u !== currentUserId,
                        );
                        if (existingReaction.users.length === 0) {
                            msg.reactions = msg.reactions?.filter(
                                (r) => r.emoji !== emoji,
                            );
                        }
                    } else {
                        // Add reaction
                        existingReaction.users.push(currentUserId);
                    }
                } else {
                    // New reaction
                    if (!msg.reactions) msg.reactions = [];
                    msg.reactions.push({ emoji, users: [currentUserId] });
                }
                return { ...msg };
            }),
        );
    };

    const handleFileUpload = () => {
        fileInputRef.current?.click();
    };

    const formatTime = (date: Date) => {
        const now = new Date();
        const diff = now.getTime() - date.getTime();
        const days = Math.floor(diff / (1000 * 60 * 60 * 24));

        if (days > 0) {
            return days === 1 ? "Yesterday" : `${days} days ago`;
        }

        return date.toLocaleTimeString([], {
            hour: "2-digit",
            minute: "2-digit",
        });
    };

    const getInitials = (name: string) => {
        return name
            .split(" ")
            .map((n) => n[0])
            .join("")
            .toUpperCase();
    };

    return (
        <div className="container mx-auto py-8 h-[calc(100vh-2rem)]">
            <div className="grid grid-cols-4 gap-6 h-full">
                {/* Members Sidebar */}
                <Card className="col-span-1">
                    <CardHeader>
                        <CardTitle className="flex items-center justify-between">
                            <div className="flex items-center gap-2">
                                <Users className="h-4 w-4" />
                                Members
                            </div>
                            <Badge variant="secondary">{members.length}</Badge>
                        </CardTitle>
                    </CardHeader>
                    <CardContent>
                        <ScrollArea className="h-[calc(100vh-12rem)]">
                            <div className="space-y-2">
                                {members.map((member) => (
                                    <div
                                        key={member.id}
                                        className="flex items-center gap-3 p-2 rounded-lg hover:bg-accent"
                                    >
                                        <div className="relative">
                                            <Avatar className="h-8 w-8">
                                                <AvatarImage
                                                    src={member.avatar}
                                                />
                                                <AvatarFallback>
                                                    {getInitials(member.name)}
                                                </AvatarFallback>
                                            </Avatar>
                                            <div
                                                className={cn(
                                                    "absolute bottom-0 right-0 h-3 w-3 rounded-full border-2 border-background",
                                                    member.status ===
                                                        "online" &&
                                                        "bg-green-500",
                                                    member.status === "away" &&
                                                        "bg-yellow-500",
                                                    member.status ===
                                                        "offline" &&
                                                        "bg-gray-400",
                                                )}
                                            />
                                        </div>
                                        <div className="flex-1 min-w-0">
                                            <div className="flex items-center gap-2">
                                                <span className="text-sm font-medium truncate">
                                                    {member.name}
                                                </span>
                                                {member.role === "owner" && (
                                                    <Badge
                                                        variant="secondary"
                                                        className="text-xs"
                                                    >
                                                        Owner
                                                    </Badge>
                                                )}
                                                {member.role === "admin" && (
                                                    <Badge
                                                        variant="outline"
                                                        className="text-xs"
                                                    >
                                                        Admin
                                                    </Badge>
                                                )}
                                            </div>
                                            {member.isTyping && (
                                                <span className="text-xs text-muted-foreground">
                                                    typing...
                                                </span>
                                            )}
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </ScrollArea>
                    </CardContent>
                </Card>

                {/* Chat Area */}
                <Card className="col-span-3 flex flex-col">
                    <CardHeader className="border-b">
                        <div className="flex items-center justify-between">
                            <div className="flex items-center gap-3">
                                <Hash className="h-5 w-5" />
                                <div>
                                    <CardTitle>Band Chat</CardTitle>
                                    <CardDescription>
                                        General discussion for all band members
                                    </CardDescription>
                                </div>
                            </div>
                            <Button
                                variant="ghost"
                                size="icon"
                                onClick={() =>
                                    setNotificationsMuted(!notificationsMuted)
                                }
                            >
                                {notificationsMuted ? (
                                    <BellOff className="h-4 w-4" />
                                ) : (
                                    <Bell className="h-4 w-4" />
                                )}
                            </Button>
                        </div>
                    </CardHeader>

                    {/* Messages */}
                    <ScrollArea className="flex-1 p-4">
                        <div className="space-y-4">
                            {messages.map((message) => {
                                const isCurrentUser =
                                    message.senderId === currentUserId;
                                const isSystem = message.type === "system";

                                if (isSystem) {
                                    return (
                                        <div
                                            key={message.id}
                                            className="text-center"
                                        >
                                            <Badge
                                                variant="secondary"
                                                className="text-xs"
                                            >
                                                {message.content}
                                            </Badge>
                                        </div>
                                    );
                                }

                                return (
                                    <div
                                        key={message.id}
                                        className={cn(
                                            "flex gap-3",
                                            isCurrentUser && "flex-row-reverse",
                                        )}
                                    >
                                        <Avatar className="h-8 w-8">
                                            <AvatarImage
                                                src={message.senderAvatar}
                                            />
                                            <AvatarFallback>
                                                {getInitials(
                                                    message.senderName,
                                                )}
                                            </AvatarFallback>
                                        </Avatar>
                                        <div
                                            className={cn(
                                                "flex-1 max-w-[70%]",
                                                isCurrentUser && "items-end",
                                            )}
                                        >
                                            <div className="flex items-baseline gap-2 mb-1">
                                                <span className="text-sm font-medium">
                                                    {message.senderName}
                                                </span>
                                                <span className="text-xs text-muted-foreground">
                                                    {formatTime(
                                                        message.timestamp,
                                                    )}
                                                </span>
                                                {message.edited && (
                                                    <span className="text-xs text-muted-foreground">
                                                        (edited)
                                                    </span>
                                                )}
                                            </div>

                                            {message.replyTo && (
                                                <div className="text-xs text-muted-foreground border-l-2 pl-2 mb-2">
                                                    Replying to{" "}
                                                    {message.replyTo.senderName}
                                                    :{" "}
                                                    {message.replyTo.content.slice(
                                                        0,
                                                        50,
                                                    )}
                                                    ...
                                                </div>
                                            )}

                                            <div
                                                className={cn(
                                                    "inline-block px-3 py-2 rounded-lg",
                                                    isCurrentUser
                                                        ? "bg-primary text-primary-foreground"
                                                        : "bg-secondary",
                                                )}
                                            >
                                                <p className="text-sm">
                                                    {message.content}
                                                </p>
                                            </div>

                                            {message.reactions &&
                                                message.reactions.length >
                                                    0 && (
                                                    <div className="flex gap-1 mt-1">
                                                        {message.reactions.map(
                                                            (reaction) => (
                                                                <Button
                                                                    key={
                                                                        reaction.emoji
                                                                    }
                                                                    variant="ghost"
                                                                    size="sm"
                                                                    className="h-6 px-1"
                                                                    onClick={() =>
                                                                        addReaction(
                                                                            message.id,
                                                                            reaction.emoji,
                                                                        )
                                                                    }
                                                                >
                                                                    <span className="text-xs">
                                                                        {
                                                                            reaction.emoji
                                                                        }{" "}
                                                                        {
                                                                            reaction
                                                                                .users
                                                                                .length
                                                                        }
                                                                    </span>
                                                                </Button>
                                                            ),
                                                        )}
                                                    </div>
                                                )}

                                            {isCurrentUser && (
                                                <div className="flex gap-1 mt-1">
                                                    <Button
                                                        variant="ghost"
                                                        size="icon"
                                                        className="h-6 w-6"
                                                        onClick={() => {
                                                            setEditingMessage(
                                                                message,
                                                            );
                                                            setInputMessage(
                                                                message.content,
                                                            );
                                                        }}
                                                    >
                                                        <Edit2 className="h-3 w-3" />
                                                    </Button>
                                                    <Button
                                                        variant="ghost"
                                                        size="icon"
                                                        className="h-6 w-6"
                                                        onClick={() =>
                                                            deleteMessage(
                                                                message.id,
                                                            )
                                                        }
                                                    >
                                                        <Trash2 className="h-3 w-3" />
                                                    </Button>
                                                </div>
                                            )}

                                            {!isCurrentUser && (
                                                <div className="flex gap-1 mt-1">
                                                    <Button
                                                        variant="ghost"
                                                        size="icon"
                                                        className="h-6 w-6"
                                                        onClick={() =>
                                                            setReplyingTo(
                                                                message,
                                                            )
                                                        }
                                                    >
                                                        <Reply className="h-3 w-3" />
                                                    </Button>
                                                    <Button
                                                        variant="ghost"
                                                        size="icon"
                                                        className="h-6 w-6"
                                                        onClick={() =>
                                                            addReaction(
                                                                message.id,
                                                                "👍",
                                                            )
                                                        }
                                                    >
                                                        <Smile className="h-3 w-3" />
                                                    </Button>
                                                </div>
                                            )}
                                        </div>
                                    </div>
                                );
                            })}
                            <div ref={messagesEndRef} />
                        </div>
                    </ScrollArea>

                    {/* Input Area */}
                    <CardContent className="border-t p-4">
                        {(editingMessage || replyingTo) && (
                            <div className="mb-2 p-2 bg-secondary rounded-lg flex justify-between items-center">
                                <div>
                                    {editingMessage && (
                                        <span className="text-sm">
                                            Editing message
                                        </span>
                                    )}
                                    {replyingTo && (
                                        <span className="text-sm">
                                            Replying to {replyingTo.senderName}:{" "}
                                            {replyingTo.content.slice(0, 50)}...
                                        </span>
                                    )}
                                </div>
                                <Button
                                    variant="ghost"
                                    size="sm"
                                    onClick={() => {
                                        setEditingMessage(null);
                                        setReplyingTo(null);
                                        setInputMessage("");
                                    }}
                                >
                                    Cancel
                                </Button>
                            </div>
                        )}

                        <div className="flex gap-2">
                            <Button
                                variant="ghost"
                                size="icon"
                                onClick={handleFileUpload}
                            >
                                <Paperclip className="h-4 w-4" />
                            </Button>
                            <input
                                ref={fileInputRef}
                                type="file"
                                className="hidden"
                                onChange={(e) => {
                                    // Handle file upload
                                    toast({
                                        title: "File upload",
                                        description:
                                            "File upload feature coming soon!",
                                    });
                                }}
                            />

                            <Input
                                value={inputMessage}
                                onChange={(e) =>
                                    setInputMessage(e.target.value)
                                }
                                onKeyPress={(e) =>
                                    e.key === "Enter" && sendMessage()
                                }
                                placeholder="Type a message..."
                                className="flex-1"
                            />

                            <Button variant="ghost" size="icon">
                                <Mic className="h-4 w-4" />
                            </Button>

                            <Button onClick={sendMessage}>
                                <Send className="h-4 w-4" />
                            </Button>
                        </div>
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}
