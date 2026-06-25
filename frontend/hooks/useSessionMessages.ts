import { useInfiniteQuery } from '@tanstack/react-query'
import { useMemo } from 'react'
import { api } from '../api/client'
import type { Message } from '../types/api'

const PAGE_SIZE = 10

export function useSessionMessages(sessionId: string, iterationId?: string) {
  const query = useInfiniteQuery({
    queryKey: ['messages', sessionId, iterationId ?? 'all'],
    queryFn: ({ pageParam }) =>
      api.getMessages(sessionId, {
        limit: PAGE_SIZE,
        before: pageParam as string | undefined,
        iterationId,
      }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) =>
      lastPage.hasMore ? lastPage.oldestCreatedAt : undefined,
    enabled: Boolean(sessionId),
  })

  const messages = useMemo(() => {
    const seen = new Set<string>()
    const merged: Message[] = []

    for (const page of query.data?.pages ?? []) {
      for (const message of page.messages) {
        if (seen.has(message.id))
          continue
        seen.add(message.id)
        merged.push(message)
      }
    }

    return merged.sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
    )
  }, [query.data?.pages])

  return {
    messages,
    hasOlderMessages: Boolean(query.hasNextPage),
    loadOlderMessages: () => void query.fetchNextPage(),
    isLoadingOlder: query.isFetchingNextPage,
    isLoading: query.isLoading,
    isError: query.isError,
    error: query.error,
    refreshMessages: () => void query.refetch(),
  }
}
