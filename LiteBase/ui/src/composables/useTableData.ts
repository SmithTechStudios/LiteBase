import { computed, type Ref } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import { fetchTables, fetchTableData, type DatabaseInfo } from '../api/litebase'

export function useTableData(selectedTable: Ref<string | null>) {
  const { data: databaseInfo, isLoading, error } = useQuery({
    queryKey: ['databaseInfo'],
    queryFn: fetchTables,
  })

  const uniqueTableNames = computed((): string[] => {
    const names = databaseInfo.value?.map((i: DatabaseInfo) => i.tableName) || []
    return Array.from(new Set(names))
  })

  const selectedTableColumns = computed((): DatabaseInfo[] => {
    return databaseInfo.value?.filter((i: DatabaseInfo) => i.tableName === selectedTable.value) || []
  })

  const visibleTableColumns = computed((): DatabaseInfo[] => {
    return selectedTableColumns.value.filter((col) => col.columnName !== 'rowid')
  })

  const {
    data: tableData,
    isLoading: tableLoading,
    error: tableError,
    refetch: refetchTableData,
  } = useQuery({
    queryKey: ['tableData', selectedTable],
    queryFn: () => fetchTableData(selectedTable.value!),
    enabled: computed(() => !!selectedTable.value),
  })

  return {
    databaseInfo,
    isLoading,
    error,
    uniqueTableNames,
    selectedTableColumns,
    visibleTableColumns,
    tableData,
    tableLoading,
    tableError,
    refetchTableData,
  }
}
