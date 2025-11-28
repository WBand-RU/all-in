'use client'

import { useState, useEffect } from 'react'
import { Button } from 'shared/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card'
import { Badge } from '@/shared/ui/badge'
import { toast } from '@/shared/ui/use-toast'
import { Trash2, Clock, Mail } from 'lucide-react'

interface Invitation {
  _id: string
  email: string
  role: string
  status: string
  createdAt: string
  expiresAt: string
  invitedBy: {
    username: string
    email: string
  }
}

interface InvitationsListProps {
  bandId: string
}

export function InvitationsList({ bandId }: InvitationsListProps) {
  const [invitations, setInvitations] = useState<Invitation[]>([])
  const [isLoading, setIsLoading] = useState(true)

  const fetchInvitations = async () => {
    try {
      const response = await fetch(`/api/bands/${bandId}/invitations`)
      if (response.ok) {
        const data = await response.json()
        setInvitations(data.invitations)
      }
    } catch (error) {
      toast({
        title: 'Ошибка',
        description: 'Не удалось загрузить приглашения',
        variant: 'destructive'
      })
    } finally {
      setIsLoading(false)
    }
  }

  const cancelInvitation = async (invitationId: string) => {
    try {
      const response = await fetch(`/api/invitations/${invitationId}`, {
        method: 'DELETE'
      })

      if (response.ok) {
        setInvitations(prev => prev.filter(inv => inv._id !== invitationId))
        toast({
          title: 'Приглашение отменено',
          description: 'Приглашение успешно отменено'
        })
      } else {
        throw new Error('Ошибка при отмене приглашения')
      }
    } catch (error) {
      toast({
        title: 'Ошибка',
        description: 'Не удалось отменить приглашение',
        variant: 'destructive'
      })
    }
  }

  useEffect(() => {
    fetchInvitations()
  }, [bandId])

  if (isLoading) {
    return <div>Загрузка...</div>
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Mail className="h-5 w-5" />
          Отправленные приглашения
        </CardTitle>
      </CardHeader>
      <CardContent>
        {invitations.length === 0 ? (
          <p className="text-muted-foreground">Нет отправленных приглашений</p>
        ) : (
          <div className="space-y-3">
            {invitations.map((invitation) => (
              <div key={invitation._id} className="flex items-center justify-between p-3 border rounded-lg">
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <span className="font-medium">{invitation.email}</span>
                    <Badge variant="secondary">{invitation.role}</Badge>
                    <Badge variant={invitation.status === 'pending' ? 'default' : 'outline'}>
                      {invitation.status}
                    </Badge>
                  </div>
                  <div className="text-sm text-muted-foreground mt-1">
                    Приглашен {invitation.invitedBy.username} • 
                    Истекает {new Date(invitation.expiresAt).toLocaleDateString()}
                  </div>
                </div>
                
                {invitation.status === 'pending' && (
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => cancelInvitation(invitation._id)}
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                )}
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
