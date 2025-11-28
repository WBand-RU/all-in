'use client'

import { useState } from 'react'
import { z } from 'zod'
import { Button } from 'shared/ui/button'
import { Input } from '@/shared/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/shared/ui/select'
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card'
import { Label } from '@/shared/ui/label'
import { toast } from '@/shared/ui/use-toast'

const inviteSchema = z.object({
  email: z.string().email('Неверный формат email'),
  role: z.string().min(1, 'Выберите роль')
})

interface InviteByEmailProps {
  bandId: string
  availableRoles: Array<{ name: string; permissions: string[] }>
  onSuccess?: () => void
}

export function InviteByEmail({ bandId, availableRoles, onSuccess }: InviteByEmailProps) {
  const [email, setEmail] = useState('')
  const [role, setRole] = useState('')
  const [isLoading, setIsLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    try {
      const data = inviteSchema.parse({ email, role })
      setIsLoading(true)

      const response = await fetch(`/api/bands/${bandId}/invitations`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
      })

      const result = await response.json()

      if (!response.ok) {
        throw new Error(result.error || 'Ошибка при отправке приглашения')
      }

      toast({
        title: 'Приглашение отправлено',
        description: `Приглашение отправлено на ${email}`
      })

      setEmail('')
      setRole('')
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

  return (
    <Card>
      <CardHeader>
        <CardTitle>Пригласить по email</CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="user@example.com"
              required
            />
          </div>

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

          <Button type="submit" disabled={isLoading} className="w-full">
            {isLoading ? 'Отправка...' : 'Отправить приглашение'}
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}
