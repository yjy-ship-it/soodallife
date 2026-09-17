export interface AuditFilterOption { value: string; label: string }
export interface AuditLogItem { occurredAt: string; adminId: string | null; adminLoginId: string; actorRole: string | null; area: string; action: string; targetType: string; targetId: string | null; reason: string | null; correlationId: string | null; result: string; beforeJson: string | null; afterJson: string | null }
export interface AuditLogResponse { totalCount: number; page: number; pageSize: number; items: AuditLogItem[]; administrators: AuditFilterOption[]; areas: AuditFilterOption[]; actions: AuditFilterOption[] }
export interface AuditFilters { from?: string; to?: string; adminId?: string; area?: string; action?: string; page?: number }
export interface SystemMetric { code: string; label: string; count: number; severity: string; path: string }
export interface OutboxStatus { status: string; count: number }
export interface OutboxFailure { id: string; aggregateType: string; eventType: string; attemptCount: number; lastAttemptAt: string | null; lastError: string | null }
export interface IntegrationStatus { code: string; label: string; status: string; note: string; deliveredCount: number; failedCount: number; successRate: number | null; lastSucceededAt: string | null; lastFailedAt: string | null }
export interface ManagedSetting { area: string; source: string; path: string; note: string }
export interface AdminAccountSummary { id: string; loginId: string; status: string; roles: string[]; createdAt: string; lastLoginAt: string | null }
export interface DatabaseStatus { connection: string; migrationStatus: string; pendingMigrationCount: number; pendingMigrations: string[] }
export interface AutomationJobStatus { name: string; configurationStatus: string; enabled: boolean; lastStartedAt: string | null; lastSucceededAt: string | null; lastFailedAt: string | null; lastErrorCode: string | null; nextScheduledAt: string | null; processingCount: number; failedCount: number }
export interface RetentionCandidateSummary { domain: string; candidateCount: number; actionStatus: string; reasonCode: string }
export interface RuntimeStatus { workingSetMb:number; managedMemoryMb:number; threadCount:number; uptimeHours:number; diskFreeMb:number; apiRequestCount24h:number; apiErrorRate24h:number|null; apiAverageDurationMs24h:number|null }
export interface JobRunItem { id:string; jobName:string; status:string; startedAt:string; completedAt:string|null; processedCount:number; failedCount:number; errorCode:string|null }
export interface RetentionPolicyItem { id:string; domainCode:string; actionCode:string; retentionDays:number; legalHoldDays:number; isEnabled:boolean; dryRun:boolean; updatedAt:string }
export interface SecurityAccount { id:string; loginId:string; status:string; detailRoleCode:string; mfaEnabled:boolean; mfaConfirmedAt:string|null; createdAt:string; lastLoginAt:string|null }
export interface SystemStatusResponse { generatedAt: string; database: DatabaseStatus; attention: SystemMetric[]; outbox: OutboxStatus[]; recentOutboxFailures: OutboxFailure[]; integrations: IntegrationStatus[]; managedSettings: ManagedSetting[]; administrators: AdminAccountSummary[]; securityLimitations: string[]; automationJobs: AutomationJobStatus[]; retentionCandidates: RetentionCandidateSummary[]; runtime:RuntimeStatus; recentJobRuns:JobRunItem[]; retentionPolicies:RetentionPolicyItem[] }
