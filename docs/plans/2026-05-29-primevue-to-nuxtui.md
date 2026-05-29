# PrimeVue → NuxtUI Migration Plan

> **For Claude:** REQUIRED SUB-SKILL: Use forge:executing-plans to implement this plan task-by-task.

**Goal:** Replace PrimeVue with NuxtUI in the LiteBase Vue UI, keeping all existing functionality intact.

**Architecture:** NuxtUI v3 ships a Vite plugin (`@nuxt/ui/vite`) that makes it fully usable in plain Vue apps without Nuxt. It brings its own icon system (`@nuxt/icon`), color mode handling, and Tailwind integration — all of which replace PrimeVue and the current VueUse dark-mode wiring. The `UTable` component is used for the data table with manual `UPagination`.

**Tech Stack:** Vue 3.5, Vite, NuxtUI v4 (`@nuxt/ui@4`), TanStack Vue Query 5, Axios, Firebase 12, dayjs

> NuxtUI v4 bundles `@nuxt/icon`, `@iconify/vue`, and `@tailwindcss/vite` — no separate installs needed for those.

---

## Task 1: Install NuxtUI and remove PrimeVue

**Files:**
- Modify: `LiteBase/ui/package.json`
- Modify: `LiteBase/ui/vite.config.ts`
- Modify: `LiteBase/ui/src/index.css`
- Modify: `LiteBase/ui/src/main.ts`

**Step 1: Install NuxtUI, remove PrimeVue packages**

```bash
cd LiteBase/ui
pnpm remove primevue @primeuix/themes
pnpm add @nuxt/ui@4
```

**Step 2: Update `vite.config.ts` to add NuxtUI plugin**

Check if `vite.config.ts` exists first (`cat LiteBase/ui/vite.config.ts`). Replace with:

```ts
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'
import ui from '@nuxt/ui/vite'

export default defineConfig({
  plugins: [
    vue(),
    tailwindcss(),
    ui(),
  ],
  build: {
    rollupOptions: {
      output: {
        entryFileNames: 'litebase.js',
        chunkFileNames: 'litebase.js',
        assetFileNames: 'litebase.css',
      },
    },
  },
})
```

Note: `ui()` must come AFTER `vue()` and `tailwindcss()`.

**Step 3: Replace `src/index.css`**

Remove all PrimeVue surface variable mappings. New content:

```css
@import "tailwindcss";
@import "@nuxt/ui";
```

**Step 4: Replace `src/main.ts`**

Remove PrimeVue registration. NuxtUI registers itself via the Vite plugin — no `app.use()` needed:

```ts
import { createApp } from 'vue'
import { VueQueryPlugin } from '@tanstack/vue-query'
import './index.css'
import App from './App.vue'

const app = createApp(App)
app.use(VueQueryPlugin)
app.mount('#app')
```

**Step 5: Verify dev server starts without errors**

```bash
pnpm dev
```

Expected: server starts, browser shows app (components will be broken until later tasks — that's fine)

**Step 6: Commit**

```bash
git add -A
git commit -m "chore: swap PrimeVue for NuxtUI, update Vite config and CSS"
```

---

## Task 2: Update `Login.vue`

**Files:**
- Modify: `LiteBase/ui/src/components/Login.vue`

PrimeVue → NuxtUI mapping:
- `Button` (primevue) → `UButton`
- `Message severity="error"` → `UAlert color="error"`
- `@iconify/vue` `Icon` → NuxtUI uses `<UIcon name="..." />` or keep `@iconify/vue` (both work)

**Step 1: Rewrite Login.vue**

```vue
<script setup lang="ts">
import { ref } from 'vue'
import { signInWithPopup, GoogleAuthProvider } from 'firebase/auth'
import { auth } from '../firebase'

const isLoading = ref(false)
const error = ref<string | null>(null)

const handleGoogleSignIn = async () => {
  isLoading.value = true
  error.value = null
  try {
    const provider = new GoogleAuthProvider()
    await signInWithPopup(auth, provider)
  } catch (err: unknown) {
    error.value = err instanceof Error ? err.message : 'Authentication failed. Please try again.'
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-indigo-100 dark:from-neutral-900 dark:to-neutral-800">
    <div class="w-full max-w-md p-8 bg-white dark:bg-neutral-800 rounded-lg shadow-xl">
      <div class="text-center mb-8">
        <h1 class="text-3xl font-bold text-neutral-800 dark:text-neutral-100 mb-2">Welcome</h1>
        <p class="text-neutral-600 dark:text-neutral-400">
          Sign in with Google to access the database explorer
        </p>
      </div>
      <div class="space-y-6">
        <UAlert v-if="error" color="error" :description="error" class="mb-4" />
        <UButton size="xl" class="w-full justify-center" :loading="isLoading" @click="handleGoogleSignIn">
          <UIcon name="devicon:google" />
          Sign in with Google
        </UButton>
      </div>
    </div>
  </div>
</template>
```

**Step 2: Check browser — login page renders correctly**

**Step 3: Commit**

```bash
git add LiteBase/ui/src/components/Login.vue
git commit -m "feat: migrate Login.vue to NuxtUI"
```

---

## Task 3: Update `Sidebar.vue`

**Files:**
- Modify: `LiteBase/ui/src/components/Sidebar.vue`

PrimeVue → NuxtUI mapping:
- `Button` → `UButton`
- `InputText` → `UInput`
- Dark mode: remove `useDark`/`useToggle` from `@vueuse/core` — use NuxtUI's `useColorMode()` instead

NuxtUI color mode:
```ts
const colorMode = useColorMode()
const isDark = computed(() => colorMode.value === 'dark')
const toggleDark = () => { colorMode.preference = isDark.value ? 'light' : 'dark' }
```

**Step 1: Rewrite Sidebar.vue**

```vue
<script setup lang="ts">
import { ref, computed } from 'vue'

const props = defineProps<{
  tableNames: string[]
  selectedTable: string | null
}>()

const emit = defineEmits<{
  selectTable: [tableName: string]
  logout: []
}>()

const searchQuery = ref('')
const isSidebarCollapsed = ref(false)

const colorMode = useColorMode()
const isDark = computed(() => colorMode.value === 'dark')
const toggleDark = () => { colorMode.preference = isDark.value ? 'light' : 'dark' }

const filteredTableNames = computed(() =>
  props.tableNames.filter(name =>
    name.toLowerCase().includes(searchQuery.value.toLowerCase())
  )
)
</script>

<template>
  <aside class="bg-neutral-100 border-r border-neutral-200 overflow-y-auto transition-all duration-300 flex flex-col">
    <div :class="['transition-all duration-300', isSidebarCollapsed ? 'p-2' : 'p-6']">
      <div :class="['flex items-center', isSidebarCollapsed ? 'justify-center' : 'justify-between']">
        <h1 v-show="!isSidebarCollapsed" class="text-2xl font-bold text-neutral-800 dark:text-neutral-100 transition-opacity duration-300">
          Litebase
        </h1>
        <div class="flex gap-2">
          <UButton variant="ghost" :aria-label="isSidebarCollapsed ? 'Expand sidebar' : 'Collapse sidebar'" @click="isSidebarCollapsed = !isSidebarCollapsed">
            <UIcon :name="isSidebarCollapsed ? 'ph:sidebar-simple-fill' : 'ph:sidebar-simple'" class="text-lg" />
          </UButton>
          <UButton v-show="!isSidebarCollapsed" variant="ghost" @click="toggleDark">
            <UIcon :name="isDark ? 'ph:sun-bold' : 'ph:moon-bold'" />
          </UButton>
          <UButton v-show="!isSidebarCollapsed" variant="ghost" title="Logout" @click="emit('logout')">
            <UIcon name="ph:sign-out" />
          </UButton>
        </div>
      </div>
    </div>

    <div v-show="!isSidebarCollapsed" class="px-6 mb-6">
      <UInput v-model="searchQuery" placeholder="Search tables" class="w-full" />
    </div>

    <nav class="flex-1">
      <ul v-show="!isSidebarCollapsed">
        <li v-for="tableName in filteredTableNames" :key="tableName"
          @click="emit('selectTable', tableName)"
          :class="['px-6 py-3 cursor-pointer transition-colors duration-200',
            selectedTable === tableName
              ? 'bg-blue-800/20 text-blue-600 font-medium'
              : 'text-neutral-600 hover:bg-neutral-50 dark:hover:bg-neutral-800']">
          {{ tableName }}
        </li>
      </ul>
      <ul v-show="isSidebarCollapsed" class="py-2">
        <li v-for="tableName in filteredTableNames" :key="tableName"
          @click="emit('selectTable', tableName)"
          :class="['flex items-center justify-center py-2 cursor-pointer transition-colors duration-200',
            selectedTable === tableName ? 'bg-blue-800/20 text-blue-600' : 'text-neutral-600 hover:bg-neutral-50']"
          :title="tableName">
          <UIcon name="ph:table" class="text-lg" />
        </li>
      </ul>
    </nav>
  </aside>
</template>
```

Note: `useColorMode()` is a NuxtUI auto-import — no import statement needed.

**Step 2: Check sidebar renders and dark mode toggle works**

**Step 3: Remove `@vueuse/core` if it's no longer used anywhere else**

```bash
grep -r "vueuse" LiteBase/ui/src/
```

If no results: `pnpm remove @vueuse/core`

**Step 4: Commit**

```bash
git add LiteBase/ui/src/components/Sidebar.vue
git commit -m "feat: migrate Sidebar.vue to NuxtUI"
```

---

## Task 4: Update `EditRowModal.vue`

**Files:**
- Modify: `LiteBase/ui/src/components/EditRowModal.vue`

PrimeVue → NuxtUI mapping:
- `Dialog` → `UModal`
- `InputText` → `UInput`
- `Button` → `UButton`

NuxtUI `UModal` uses a different open/close pattern — it wraps content in a slot and is controlled via `v-model`:

```vue
<UModal v-model:open="visible">
  <template #content>
    ...body...
    <template #footer>...</template>
  </template>
</UModal>
```

**Step 1: Rewrite EditRowModal.vue**

```vue
<script setup lang="ts">
import { ref } from 'vue'
import type { DatabaseInfo } from '../api/litebase'

const props = defineProps<{
  columns: DatabaseInfo[]
}>()

const emit = defineEmits<{
  save: [rowid: unknown, body: Record<string, unknown>]
}>()

const open = ref(false)
const editRow = ref<Record<string, unknown>>({})
const saving = ref(false)
const error = ref<string | null>(null)

const openModal = (row: Record<string, unknown>) => {
  editRow.value = { ...row }
  error.value = null
  open.value = true
}

const save = async () => {
  saving.value = true
  error.value = null
  try {
    const rowid = editRow.value['rowid']
    const pkColumns = new Set(props.columns.filter(c => c.isPrimaryKey !== 0).map(c => c.columnName))
    const body = Object.fromEntries(
      Object.entries(editRow.value).filter(([k]) => k !== 'rowid' && !pkColumns.has(k))
    )
    emit('save', rowid, body)
    open.value = false
  } finally {
    saving.value = false
  }
}

const setError = (msg: string) => { error.value = msg }

defineExpose({ openModal, setError })
</script>

<template>
  <UModal v-model:open="open" title="Edit Row">
    <template #body>
      <div class="flex flex-col gap-4">
        <div v-for="col in columns" :key="col.columnName" class="flex flex-col gap-1">
          <label class="text-sm font-medium text-neutral-700 dark:text-neutral-300">
            {{ col.columnName }}
            <span class="text-neutral-400 font-normal ml-1">({{ col.columnType }})</span>
            <span v-if="col.isPrimaryKey !== 0" class="text-xs text-blue-500 ml-1">PK</span>
          </label>
          <UInput v-if="col.columnType !== 'BLOB'"
            v-model="(editRow as Record<string, unknown>)[col.columnName] as string"
            :type="col.columnType === 'INTEGER' || col.columnType === 'REAL' ? 'number' : 'text'"
            :disabled="col.isPrimaryKey !== 0"
            class="w-full" />
          <UInput v-else disabled model-value="[BLOB data]" class="w-full opacity-50" />
        </div>
        <UAlert v-if="error" color="error" :description="error" />
      </div>
    </template>
    <template #footer>
      <div class="flex justify-end gap-2">
        <UButton variant="ghost" :disabled="saving" @click="open = false">Cancel</UButton>
        <UButton :loading="saving" @click="save">Save</UButton>
      </div>
    </template>
  </UModal>
</template>
```

**Step 2: Update `TableView.vue` — change `editModal?.open(...)` to `editModal?.openModal(...)`** (method was renamed to avoid clash with NuxtUI's `open` ref)

**Step 3: Check modal opens, form renders, save works**

**Step 4: Commit**

```bash
git add LiteBase/ui/src/components/EditRowModal.vue LiteBase/ui/src/components/TableView.vue
git commit -m "feat: migrate EditRowModal.vue to NuxtUI UModal"
```

---

## Task 5: Update `TableView.vue` — replace DataTable with UTable + UPagination

**Files:**
- Modify: `LiteBase/ui/src/components/TableView.vue`

This is the most significant change. NuxtUI `UTable` API:
- `columns` prop: `Array<{ key: string, label: string, sortable?: boolean }>`
- `rows` prop: array of data objects
- No built-in pagination — wire `UPagination` manually with a computed slice

PrimeVue → NuxtUI mapping:
- `ProgressSpinner` → `USkeleton` or just a `UIcon` spinner
- `Button` → `UButton`
- `DataTable` + `Column` → `UTable` + `UPagination`

**Step 1: Rewrite TableView.vue**

```vue
<script setup lang="ts">
import { ref, computed } from 'vue'
import axios from 'axios'
import dayjs from 'dayjs'
import { updateRow, type DatabaseInfo } from '../api/litebase'
import EditRowModal from './EditRowModal.vue'

const props = defineProps<{
  tableName: string
  columns: DatabaseInfo[]
  visibleColumns: DatabaseInfo[]
  tableData: Record<string, unknown>[] | undefined
  tableLoading: boolean
  tableError: unknown
}>()

const emit = defineEmits<{
  refetch: []
}>()

const editModal = ref<InstanceType<typeof EditRowModal> | null>(null)

const page = ref(1)
const pageSize = ref(10)

const tableColumns = computed(() => [
  { key: 'actions', label: '' },
  ...props.visibleColumns.map(col => ({ key: col.columnName, label: col.columnName, sortable: true }))
])

const paginatedRows = computed(() => {
  const data = props.tableData ?? []
  const start = (page.value - 1) * pageSize.value
  return data.slice(start, start + pageSize.value)
})

const friendlyError = (err: unknown): string => {
  if (axios.isAxiosError(err) && err.response?.status === 401)
    return 'Access denied. You are not authorised to view this database.'
  return err instanceof Error ? err.message : 'An unexpected error occurred.'
}

const formatCell = (col: DatabaseInfo, value: unknown): string | null => {
  if (typeof value !== 'string' && typeof value !== 'number') return String(value ?? '')
  if (col.columnType === 'TEXT' && typeof value === 'string' && dayjs(value).isValid()) {
    return dayjs(value).format('DD-MM-YYYY HH:mm')
  }
  if (typeof value === 'number' && !Number.isInteger(value)) return value.toFixed(2)
  return String(value).substring(0, 200)
}

const isImageUrl = (col: DatabaseInfo, value: unknown) =>
  col.columnType === 'TEXT' && typeof value === 'string' && value.includes('https://')

const handleSave = async (rowid: unknown, body: Record<string, unknown>) => {
  try {
    await updateRow(props.tableName, rowid, body)
    emit('refetch')
  } catch (e: unknown) {
    editModal.value?.setError(e instanceof Error ? e.message : 'Failed to save changes')
  }
}
</script>

<template>
  <div class="shadow-sm rounded-lg overflow-hidden border border-neutral-200 bg-white dark:bg-neutral-900">
    <div class="px-6 py-4 border-b border-neutral-200 flex justify-between items-center bg-gradient-to-r from-blue-800/20 to-indigo-800/20">
      <h2 class="text-xl font-semibold text-neutral-800 dark:text-neutral-100">{{ tableName }}</h2>
      <UButton variant="ghost" icon="i-ph-arrows-clockwise" :loading="tableLoading" @click="emit('refetch')" />
    </div>

    <div class="p-6">
      <h3 class="text-lg font-semibold mb-4 text-neutral-700 dark:text-neutral-300">Table Columns</h3>
      <ul class="mb-8 grid grid-cols-2 gap-2">
        <li v-for="column in columns" :key="column.columnName" class="bg-neutral-100 dark:bg-neutral-800 rounded-md p-2 text-sm">
          <span class="font-medium text-neutral-700 dark:text-neutral-300">{{ column.columnName }}</span>
          <span class="text-neutral-500 ml-2">({{ column.columnType }})</span>
        </li>
      </ul>

      <h3 class="text-lg font-semibold mb-4 text-neutral-700 dark:text-neutral-300">Table Data</h3>

      <div v-if="tableLoading" class="flex justify-center py-8">
        <UIcon name="ph:spinner" class="animate-spin text-3xl text-neutral-400" />
      </div>
      <div v-else-if="tableError" class="flex flex-col items-center justify-center gap-3 mt-8 text-center">
        <UIcon
          :name="axios.isAxiosError(tableError) && tableError.response?.status === 401 ? 'ph:lock-bold' : 'ph:warning-circle-bold'"
          class="text-4xl text-neutral-400" />
        <p class="text-neutral-600 font-medium">{{ friendlyError(tableError) }}</p>
      </div>
      <div v-else>
        <UTable :columns="tableColumns" :rows="paginatedRows">
          <template #actions-data="{ row }">
            <UButton variant="ghost" size="xs" icon="i-ph-pencil-simple"
              @click="editModal?.openModal(row as Record<string, unknown>)" />
          </template>
          <template v-for="col in visibleColumns" #[`${col.columnName}-data`]="{ row }" :key="col.columnName">
            <img v-if="isImageUrl(col, row[col.columnName])" class="size-15" :src="row[col.columnName] as string" />
            <span v-else class="line-clamp-2 truncate">{{ formatCell(col, row[col.columnName]) }}</span>
          </template>
        </UTable>

        <div class="flex justify-between items-center mt-4">
          <USelect v-model="pageSize" :options="[10, 20, 50]" class="w-24" />
          <UPagination
            v-model:page="page"
            :total="tableData?.length ?? 0"
            :items-per-page="pageSize"
          />
        </div>

        <EditRowModal ref="editModal" :columns="visibleColumns" @save="handleSave" />
      </div>
    </div>
  </div>
</template>
```

**Step 2: Verify table renders with data, pagination works, edit modal opens from pencil button**

**Step 3: Commit**

```bash
git add LiteBase/ui/src/components/TableView.vue
git commit -m "feat: migrate TableView.vue to NuxtUI UTable + UPagination"
```

---

## Task 6: Update `App.vue` — replace ProgressSpinner and error icons

**Files:**
- Modify: `LiteBase/ui/src/App.vue`

PrimeVue → NuxtUI mapping:
- `ProgressSpinner` → `<UIcon name="ph:spinner" class="animate-spin" />`
- Remove `import ProgressSpinner from 'primevue/progressspinner'`

**Step 1: Update App.vue**

```vue
<script setup lang="ts">
import { ref } from 'vue'
import axios from 'axios'
import Login from './components/Login.vue'
import Sidebar from './components/Sidebar.vue'
import TableView from './components/TableView.vue'
import { useAuth } from './composables/useAuth'
import { useTableData } from './composables/useTableData'

const { user, authLoading, handleLogout } = useAuth()
const selectedTable = ref<string | null>(null)

const {
  isLoading,
  error,
  uniqueTableNames,
  selectedTableColumns,
  visibleTableColumns,
  tableData,
  tableLoading,
  tableError,
  refetchTableData,
} = useTableData(selectedTable)

const friendlyError = (err: unknown): string => {
  if (axios.isAxiosError(err) && err.response?.status === 401)
    return 'Access denied. You are not authorised to view this database.'
  return err instanceof Error ? err.message : 'An unexpected error occurred.'
}
</script>

<template>
  <div v-if="authLoading" class="flex items-center justify-center h-screen">
    <UIcon name="ph:spinner" class="animate-spin text-4xl text-neutral-400" />
  </div>

  <Login v-else-if="!user" />

  <div v-else class="grid h-screen overflow-hidden bg-white dark:bg-neutral-900 transition-all duration-300"
    style="grid-template-columns: auto 1fr">
    <Sidebar
      :table-names="uniqueTableNames"
      :selected-table="selectedTable"
      @select-table="selectedTable = $event"
      @logout="handleLogout"
    />
    <main class="bg-neutral-100 dark:bg-neutral-900 overflow-y-auto">
      <div class="container mx-auto px-8 py-10">
        <div v-if="isLoading" class="flex items-center justify-center h-full">
          <UIcon name="ph:spinner" class="animate-spin text-4xl text-neutral-400" />
        </div>
        <div v-else-if="error" class="flex flex-col items-center justify-center gap-3 mt-16 text-center">
          <UIcon
            :name="axios.isAxiosError(error) && error.response?.status === 401 ? 'ph:lock-bold' : 'ph:warning-circle-bold'"
            class="text-5xl text-neutral-400" />
          <p class="text-neutral-600 font-medium">{{ friendlyError(error) }}</p>
        </div>
        <TableView v-else-if="selectedTable"
          :table-name="selectedTable"
          :columns="selectedTableColumns"
          :visible-columns="visibleTableColumns"
          :table-data="tableData"
          :table-loading="tableLoading"
          :table-error="tableError"
          @refetch="refetchTableData"
        />
        <div v-else class="text-center text-neutral-500 mt-8">
          Select a table from the sidebar to view its data.
        </div>
      </div>
    </main>
  </div>
</template>
```

**Step 2: Run `pnpm vue-tsc --noEmit` — should be clean**

**Step 3: Full smoke test in browser:**
- Login page renders
- Table list loads in sidebar
- Dark mode toggles
- Table data displays with pagination
- Edit modal opens, saves, refreshes
- 401 error shows lock icon

**Step 4: Run production build**

```bash
pnpm build
```

Expected: `dist/litebase.js` and `dist/litebase.css` generated successfully.

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: complete PrimeVue → NuxtUI migration"
```
