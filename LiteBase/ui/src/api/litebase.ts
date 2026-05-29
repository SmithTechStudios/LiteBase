import axios from 'axios'
import { config } from '../config'

export interface DatabaseInfo {
  tableName: string
  columnName: string
  columnType: string
  isPrimaryKey: number
}

export const apiClient = axios.create({
  baseURL: config.apiBaseUrl,
  headers: { 'Content-Type': 'application/json' },
})

export const fetchTables = async (): Promise<DatabaseInfo[]> => {
  const response = await apiClient.get<DatabaseInfo[]>('/LiteBase/tables')
  return response.data
}

export const fetchTableData = async (tableName: string): Promise<Record<string, unknown>[]> => {
  const response = await apiClient.get(`/LiteBase/table/${tableName}`)
  return response.data
}

export const updateRow = async (tableName: string, rowid: unknown, body: Record<string, unknown>): Promise<void> => {
  await apiClient.put(`/LiteBase/table/${tableName}/${rowid}`, body)
}
