export interface AdminProviderRequirementType { code: string; name: string; isActive: boolean }
export interface AdminProviderRequirementDefinition { id: string; requirementTypeCode: string; requirementCode: string; name: string; description: string | null; isActive: boolean }
export interface AdminProviderDocumentType { id: string; code: string; name: string; supportsExpiry: boolean; isActive: boolean }
export interface AdminProviderRequirementStandards { requirementTypes: AdminProviderRequirementType[]; requirementDefinitions: AdminProviderRequirementDefinition[]; documentTypes: AdminProviderDocumentType[] }
export interface SaveProviderRequirementTypeInput { code: string; name: string; isActive: boolean }
export interface SaveProviderRequirementDefinitionInput { requirementTypeCode: string; requirementCode: string; name: string; description: string | null; isActive: boolean }
export interface SaveProviderDocumentTypeInput { code: string; name: string; supportsExpiry: boolean; isActive: boolean }
export interface AdminProviderRequirementDefaults { createdRequirementTypeCount: number; createdRequirementDefinitionCount: number; createdDocumentTypeCount: number; existingRequirementTypeCount: number; existingRequirementDefinitionCount: number; existingDocumentTypeCount: number }
