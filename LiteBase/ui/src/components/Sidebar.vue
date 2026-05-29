<script setup lang="ts">
import { ref, computed } from 'vue'
import { useDark } from '@vueuse/core'

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

const isDark = useDark()
const toggleDark = () => { isDark.value = !isDark.value }

const filteredTableNames = computed(() =>
  props.tableNames.filter(name =>
    name.toLowerCase().includes(searchQuery.value.toLowerCase())
  )
)
</script>

<template>
  <aside class="flex flex-col border-r border-default bg-elevated transition-all duration-300"
    :class="isSidebarCollapsed ? 'w-14' : 'w-64'">

    <div class="flex items-center border-b border-default p-3 gap-2"
      :class="isSidebarCollapsed ? 'justify-center' : 'justify-between'">
      <span v-show="!isSidebarCollapsed" class="font-bold text-lg px-1">Litebase</span>
      <div class="flex gap-1">
        <UButton
          variant="ghost"
          color="neutral"
          :icon="isSidebarCollapsed ? 'ph:sidebar-simple-fill' : 'ph:sidebar-simple'"
          :aria-label="isSidebarCollapsed ? 'Expand sidebar' : 'Collapse sidebar'"
          @click="isSidebarCollapsed = !isSidebarCollapsed"
        />
        <template v-if="!isSidebarCollapsed">
          <UButton
            variant="ghost"
            color="neutral"
            :icon="isDark ? 'ph:sun-bold' : 'ph:moon-bold'"
            @click="toggleDark"
          />
          <UButton
            variant="ghost"
            color="neutral"
            icon="ph:sign-out"
            aria-label="Logout"
            @click="emit('logout')"
          />
        </template>
      </div>
    </div>

    <div v-show="!isSidebarCollapsed" class="p-3">
      <UInput v-model="searchQuery" placeholder="Search tables" icon="ph:magnifying-glass" />
    </div>

    <nav class="flex-1 overflow-y-auto">
      <ul v-show="!isSidebarCollapsed">
        <li v-for="tableName in filteredTableNames" :key="tableName">
          <UButton
            block
            variant="ghost"
            color="neutral"
            class="justify-start rounded-none"
            :class="selectedTable === tableName ? 'bg-accented font-medium' : ''"
            @click="emit('selectTable', tableName)"
          >
            {{ tableName }}
          </UButton>
        </li>
      </ul>
      <ul v-show="isSidebarCollapsed" class="flex flex-col items-center gap-1 p-2">
        <li v-for="tableName in filteredTableNames" :key="tableName">
          <UButton
            variant="ghost"
            color="neutral"
            icon="ph:table"
            :aria-label="tableName"
            :class="selectedTable === tableName ? 'bg-accented' : ''"
            @click="emit('selectTable', tableName)"
          />
        </li>
      </ul>
    </nav>
  </aside>
</template>
