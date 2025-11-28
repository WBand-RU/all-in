'use client'

import { useState } from 'react'
import { z } from 'zod'
import { Button } from 'shared/ui/button'
import { Input } from '@/shared/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/shared/ui/select'
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card'
import { Label } from '@/shared/ui/label'
import { toast } from '@/shared/ui/use-toast'
import { Copy, ExternalLink } from 'lucide-react'

const createTokenSchema = z.object({
  role: z.string().min(1, 'Выберите роль'),
  maxUses: z.number().min(1).max(100),
  expiresInDays: z.number().min(1).max(30)
})

interface CreateInviteLinkProps {
  bandId: string
  availableRoles: Array<{ name: string; permissions: string[] }>
  onSuccess?: () => void
}

export function CreateInviteLink({ bandId, availableRoles, onSuccess }: CreateInviteLinkProps) {
  const [role, setRole] = useState('')
  const [maxUses, setMaxUses] = useState(1)
  const [expiresInDays, setExpiresInDays] = useState(7)
  const [isLoading, setIsLoading] = useState(false)
  const [generatedLink, setGeneratedLink] = useState('')

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    try {
      const data = createTokenSchema.parse({ role, maxUses, expiresInDays })
      setIsLoading(true)

      const response = await fetch(`/api/bands/${bandId}/invite-tokens`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
      })

      const result = await response.json()

      if (!response.ok) {
        throw new Error(result.error || 'Ошибка при создании ссылки')
      }

      const inviteUrl = `${window.location.origin}/join/${result.inviteToken.token}`
      setGeneratedLink(inviteUrl)

      toast({
        title: 'Ссылка создана',
        description: 'Одноразовая ссылка для приглашения создана'
      })

      onSuccess?.()

    } catch (error) {
      if (error instanceof z.ZodError) {
        toast({
          title: 'Ошибка валидации',
          description: error.errors[0].message,
          variant: 'destructive'
        })
      } else {
        toast({
          title: 'Ошибка',
          description: error instanceof Error ? error.message : 'Неизвестная ошибка',
          variant: 'destructive'
        })
      }
    } finally {
      setIsLoading(false)
    }
  }

  const copyToClipboard = async () => {
    try {
      await navigator.clipboard.writeText(generatedLink)
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

  return (
    <Card>
      <CardHeader>
        <CardTitle>Создать ссылку-приглашение</CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="role">Роль</Label>
            <Select value={role} onValueChange={setRole} required>
              <SelectTrigger>
                <SelectValue placeholder="Выберите роль" />
              </SelectTrigger>
              <SelectContent>
                {availableRoles.map((r) => (
                  <SelectItem key={r.name} value={r.name}>
                    {r.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="maxUses">Максимум использований</Label>
              <Input
                id="maxUses"
                type="number"
                min="1"
                max="100"
                value={maxUses}
                onChange={(e) => setMaxUses(parseInt(e.target.value))}
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="expiresInDays">Срок действия (дни)</Label>
              <Input
                id="expiresInDays"
                type="number"
                min="1"
                max="30"
                value={expiresInDays}
                onChange={(e) => setExpiresInDays(parseInt(e.target.value))}
                required
              />
            </div>
          </div>

          <Button type="submit" disabled={isLoading} className="w-full">
            {isLoading ? 'Создание...' : 'Создать ссылку'}
          </Button>
        </form>

        {generatedLink && (
          <div className="mt-4 p-4 bg-gray-50 rounded-lg">
            <Label className="text-sm font-medium">Ссылка для приглашения:</Label>
            <div className="flex items-center gap-2 mt-2">
              <Input
                value={generatedLink}
                readOnly
                className="flex-1"
              />
              <Button size="sm" onClick={copyToClipboard}>
                <Copy className="h-4 w-4" />
              </Button>
              <Button 
                size="sm" 
                variant="outline"
                onClick={() => window.open(generatedLink, '_blank')}
              >
                <ExternalLink className="h-4 w-4" />
              </Button>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
