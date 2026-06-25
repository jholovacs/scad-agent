import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { SessionList } from '../components/SessionList'

export function HomePage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const sessionsQuery = useQuery({ queryKey: ['sessions'], queryFn: api.listSessions })

  const createMutation = useMutation({
    mutationFn: () => api.createSession(),
    onSuccess: (session) => {
      void queryClient.invalidateQueries({ queryKey: ['sessions'] })
      void navigate(`/sessions/${session.id}`)
    },
  })

  return (
    <main className="page">
      <SessionList
        sessions={sessionsQuery.data ?? []}
        onCreate={() => createMutation.mutate()}
        creating={createMutation.isPending}
      />
    </main>
  )
}
