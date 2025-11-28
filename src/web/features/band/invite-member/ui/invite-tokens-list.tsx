'use client'

import { useState, useEffect } from 'react'
import { Button } from 'shared/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card'
import { Badge } from '@/shared/ui/badge'
import { toast } from '@/shared/ui/use-toast'
import { Copy, ExternalLink, Trash2, Link2 } from 'lucide-react'

interface InviteToken {
  _id: string
  token: string
  role: string
  maxUses: number
  currentUses: number
  expiresAt: string
  isActive: boolean
  createdAt: string
  createdBy: {
    username: string
  }
}

interface InviteTokensListProps {
  bandId: string
}

export function InviteTokensList({ bandId }: InviteTokensListProps) {
  const [tokens, setTokens] = useState<InviteToken[]>([])
  const [isLoading, setIsLoading] = useState(true)

  const fetchTokens = async () => {
    try {
      const response = await fetch(`/api/bands/${bandId}/invite-tokens`)
      if (response.ok) {
        const data = await response.json()
        setTokens(data.tokens)
      }
    } catch (error) {
      toast({
        title: 'Ошибка',
        description: 'Не удалось загрузить токены',
        variant: 'destructive'
      })
    } finally {
      setIsLoading(false)
    }
  }

  const deactivateToken = async (tokenId: string) => {
    try {
      const response = await fetch(`/api/bands/${bandId}/invite-tokens?tokenId=${tokenId}`, {
        method: 'DELETE'
      })

      if (response.ok) {
        setTokens(prev => prev.map(token => 
          token._id === tokenId ? { ...token, isActive: false } : token
        ))
        toast({
          title: 'Токен деактивирован',
          description: 'Ссылка больше не действительна'
        })
      } else {
        throw new Error('Ошибка при деактивации токена')
      }
    } catch (error) {
      toast({
        title: 'Ошибка',
        description: 'Не удалось деактивировать токен',
        variant: 'destructive'
      })
    }
  }

  const copyLink = async (token: string) => {
    const inviteUrl = `${window.location.origin}/join/${token}`
    try {
      await navigator.clipboard.writeText(inviteUrl)
      toast({
        title: 'Скопировано',
        description: 'Ссылка скопирована в буфер обмена'
      })
    } catch (error) {
      toast({
        title: 'Ошибка',
        description: 'Не удалось скопировать ссылку',
        variant: 'destructive'
      })
    }
  }

  const openLink = (token: string) => {
    const inviteUrl = `${window.location.origin}/join/${token}`
    window.open(inviteUrl, '_blank')
  }

  useEffect(() => {
    fetchTokens()
  }, [bandId])

  if (isLoading) {
    return <div>Загрузка...</div>
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Link2 className="h-5 w-5" />
          Ссылки-приглашения
        </CardTitle>
      </CardHeader>
      <CardContent>
        {tokens.length === 0 ? (
          <p className="text-muted-foreground">Нет активных ссылок</p>
        ) : (
          <div className="space-y-3">
            {tokens.map((token) => (
              <div key={token._id} className="flex items-center justify-between p-3 border rounded-lg">
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <Badge variant="secondary">{token.role}</Badge>
                    <Badge variant={token.isActive ? 'default' : 'outline'}>
                      {token.isActive ? 'активен' : 'деактивирован'}
                    </Badge>
                    <span className="text-sm text-muted-foreground">
                      {token.currentUses}/{token.maxUses} использований
                    </span>
                  </div>
                  <div className="text-sm text-muted-foreground mt-1">
                    Создан {token.createdBy.username} • 
                    Истекает {new Date(token.expiresAt).toLocaleDateString()}
                  </div>
                </div>
                
                <div className="flex items-center gap-2">
                  {token.isActive && (
                    <>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => copyLink(token.token)}
                      >
                        <Copy className="h-4 w-4" />
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => openLink(token.token)}
                      >
                        <ExternalLink className="h-4 w-4" />
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => deactivateToken(token._id)}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
