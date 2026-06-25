import { useInfiniteQuery } from '@tanstack/react-query'
import { useMemo } from 'react'
import { api } from '../api/client'
import type { Iteration } from '../types/api'

const PAGE_SIZE = 10

export function useSessionIterations(sessionId: string) {
  const query = useInfiniteQuery({
    queryKey: ['iterations', sessionId],
    queryFn: ({ pageParam }) =>
      api.getIterations(sessionId, {
        limit: PAGE_SIZE,
        beforeVersion: pageParam as number | undefined,
      }),
    initialPageParam: undefined as number | undefined,
    getNextPageParam: (lastPage) =>
      lastPage.hasMore ? lastPage.oldestVersion : undefined,
    enabled: Boolean(sessionId),
  })

  const iterations = useMemo(() => {
    const seen = new Set<string>()
    const merged: Iteration[] = []

    for (const page of query.data?.pages ?? []) {
      for (const iteration of page.iterations) {
        if (seen.has(iteration.id))
          continue
        seen.add(iteration.id)
        merged.push(iteration)
      }
    }

    return merged.sort((a, b) => b.version - a.version)
  }, [query.data?.pages])

  return {
    iterations,
    hasOlderIterations: Boolean(query.hasNextPage),
    loadOlderIterations: () => void query.fetchNextPage(),
    isLoadingOlder: query.isFetchingNextPage,
    isLoading: query.isLoading,
    isError: query.isError,
    error: query.error,
    refreshIterations: () => void query.refetch(),
  }
}
