export type LlmProvider = 1 | 2; // 1: Gemini, 2: Nvidia

export interface ProjectSummary {
  projectId: string;
  projectName: string;
  sourceFileName: string;
  sourceType: string;
  createdAtUtc: string;
  storeyCount: number;
  spaceCount: number;
  doorCount: number;
  wallCount: number;
  totalWallAreaM2: number;
  totalWallVolumeM3: number;
}

export interface AuditFinding {
  id: string;
  elementGlobalId: string;
  elementType: string;
  elementName: string;
  storeyName: string;
  severity: 'Error' | 'Warning' | 'Passed';
  ruleReference: string;
  description: string;
  suggestedFix: string;
  llmProviderUsed: string;
  auditedAtUtc: string;
}

export interface ChatResponse {
  reply: string;
  providerUsed: string;
}

export interface RegulationSearchResult {
  ruleCode: string;
  category: string;
  content: string;
  score: number;
}

const API_BASE_URL = 'https://localhost:7002/api';

export const archGuardApi = {
  async getProjects(): Promise<ProjectSummary[]> {
    const res = await fetch(`${API_BASE_URL}/Projects`);
    if (!res.ok) throw new Error('Projeler alınamadı.');
    return res.json();
  },

  async uploadIfc(file: File): Promise<ProjectSummary> {
    const formData = new FormData();
    formData.append('file', file);
    const res = await fetch(`${API_BASE_URL}/Projects/upload-ifc`, {
      method: 'POST',
      body: formData,
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async getFindings(projectId: string): Promise<AuditFinding[]> {
    const res = await fetch(`${API_BASE_URL}/Audit/${projectId}`);
    if (!res.ok) throw new Error('Denetim bulguları alınamadı.');
    return res.json();
  },

  async runAudit(projectId: string, provider: LlmProvider): Promise<AuditFinding[]> {
    const res = await fetch(`${API_BASE_URL}/Audit/run`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ projectId, provider }),
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async chatWithModel(projectId: string, message: string, provider: LlmProvider): Promise<ChatResponse> {
    const res = await fetch(`${API_BASE_URL}/Audit/chat`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ projectId, message, provider }),
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async uploadRegulationPdf(file: File, category: string): Promise<{ message: string }> {
    const formData = new FormData();
    formData.append('file', file);
    const res = await fetch(`${API_BASE_URL}/Regulations/upload-pdf?category=${encodeURIComponent(category)}`, {
      method: 'POST',
      body: formData,
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async searchRegulations(query: string): Promise<RegulationSearchResult[]> {
    const res = await fetch(`${API_BASE_URL}/Regulations/search?query=${encodeURIComponent(query)}&topK=4`);
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  }
};