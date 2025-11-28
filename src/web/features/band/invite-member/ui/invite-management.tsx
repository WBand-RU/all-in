'use client'

import { useState } from 'react'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/ui/tabs'
import { InviteByEmail } from './invite-by-email'
import { CreateInviteLink } from './create-invite-link'
import { InvitationsList } from './invitations-list'
import { InviteTokensList } from './invite-tokens-list'

interface InviteManagementProps {
  bandId: string
  availableRoles: Array<{ name: string; permissions: string[] }>
}

export function InviteManagement({ bandId, availableRoles }: InviteManagementProps) {
  const [refreshKey, setRefreshKey] = useState(0)

  const handleSuccess = () => {
    setRefreshKey(prev => prev + 1)
  }

  return (
    <div className="space-y-6">
      <Tabs defaultValue="email" className="w-full">
        <TabsList className="grid w-full grid-cols-2">
          <TabsTrigger value="email">По Email</TabsTrigger>
          <TabsTrigger value="link">По ссылке</TabsTrigger>
        </TabsList>

        <TabsContent value="email" className="space-y-4">
          <InviteByEmail 
            bandId={bandId} 
            availableRoles={availableRoles}
            onSuccess={handleSuccess}
          />
          <InvitationsList key={`invitations-${refreshKey}`} bandId={bandId} />
        </TabsContent>

        <TabsContent value="link" className="space-y-4">
          <CreateInviteLink 
            bandId={bandId} 
            availableRoles={availableRoles}
            onSuccess={handleSuccess}
          />
          <InviteTokensList key={`tokens-${refreshKey}`} bandId={bandId} />
        </TabsContent>
      </Tabs>
    </div>
  )
}
