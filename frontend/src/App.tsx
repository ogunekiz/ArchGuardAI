import React, { useCallback, useEffect, useState } from 'react';
import {
  Building2,
  Upload,
  ShieldCheck,
  AlertTriangle,
  XCircle,
  CheckCircle2,
  Bot,
  Send,
  FileText,
  Layers,
  DoorOpen,
  Box,
  Cpu,
  Search,
  Loader2
} from 'lucide-react';
import { archGuardApi } from './api/archGuardApi';
import type {
  AuditFinding,
  LlmProvider,
  ProjectSummary,
  RegulationSearchResult
} from './api/archGuardApi';

function getErrorMessage(err: unknown): string {
  if (err instanceof Error) return err.message;
  return String(err);
}

export function App() {
  const [projects, setProjects] = useState<ProjectSummary[]>([]);
  const [selectedProject, setSelectedProject] = useState<ProjectSummary | null>(null);
  const [provider, setProvider] = useState<LlmProvider>(1); // 1: Gemini, 2: Nvidia
  const [activeTab, setActiveTab] = useState<'audit' | 'chat' | 'rag'>('audit');

  // States for Data
  const [findings, setFindings] = useState<AuditFinding[]>([]);
  const [severityFilter, setSeverityFilter] = useState<'ALL' | 'Error' | 'Warning' | 'Passed'>('ALL');
  const [isUploading, setIsUploading] = useState(false);
  const [isAuditing, setIsAuditing] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  // Chat States
  const [chatHistory, setChatHistory] = useState<{ role: 'user' | 'ai'; text: string; provider?: string }[]>([]);
  const [chatInput, setChatInput] = useState('');
  const [isChatting, setIsChatting] = useState(false);

  // RAG States
  const [ragQuery, setRagQuery] = useState('');
  const [ragResults, setRagResults] = useState<RegulationSearchResult[]>([]);
  const [pdfCategory, setPdfCategory] = useState('İmar ve Yangın Yönetmeliği');
  const [isPdfUploading, setIsPdfUploading] = useState(false);

  const loadProjects = useCallback(async () => {
    try {
      const list = await archGuardApi.getProjects();
      setProjects(list);
      setSelectedProject((prev) => prev ?? (list.length > 0 ? list[0] : null));
    } catch (err: unknown) {
      setErrorMsg(getErrorMessage(err));
    }
  }, []);

  useEffect(() => {
    void loadProjects();
  }, [loadProjects]);

  useEffect(() => {
    if (selectedProject) {
      void loadFindings(selectedProject.projectId);
      setChatHistory([
        {
          role: 'ai',
          text: `"${selectedProject.projectName}" modeli yüklendi (${selectedProject.doorCount} kapı, ${selectedProject.wallCount} duvar, ${selectedProject.spaceCount} mahal). Metraj veya yönetmelik uyumluluğu hakkında soru sorabilirsiniz.`
        }
      ]);
    }
  }, [selectedProject]);

  const loadFindings = async (projectId: string) => {
    try {
      const data = await archGuardApi.getFindings(projectId);
      setFindings(data);
    } catch {
      setFindings([]);
    }
  };

  const handleIfcUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setIsUploading(true);
    setErrorMsg(null);
    try {
      const created = await archGuardApi.uploadIfc(file);
      setProjects((prev) => [created, ...prev]);
      setSelectedProject(created);
    } catch (err: unknown) {
      setErrorMsg(`IFC Yükleme Hatası: ${getErrorMessage(err)}`);
    } finally {
      setIsUploading(false);
      e.target.value = '';
    }
  };

  const handleRunAudit = async () => {
    if (!selectedProject) return;
    setIsAuditing(true);
    setErrorMsg(null);
    try {
      const result = await archGuardApi.runAudit(selectedProject.projectId, provider);
      setFindings(result);
      setActiveTab('audit');
    } catch (err: unknown) {
      setErrorMsg(`Denetim Hatası: ${getErrorMessage(err)}`);
    } finally {
      setIsAuditing(false);
    }
  };

  const handleSendChat = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedProject || !chatInput.trim() || isChatting) return;

    const userText = chatInput.trim();
    setChatInput('');
    setChatHistory((prev) => [...prev, { role: 'user', text: userText }]);
    setIsChatting(true);

    try {
      const res = await archGuardApi.chatWithModel(selectedProject.projectId, userText, provider);
      setChatHistory((prev) => [...prev, { role: 'ai', text: res.reply, provider: res.providerUsed }]);
    } catch (err: unknown) {
      setChatHistory((prev) => [...prev, { role: 'ai', text: `Hata: ${getErrorMessage(err)}` }]);
    } finally {
      setIsChatting(false);
    }
  };

  const handlePdfUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setIsPdfUploading(true);
    setErrorMsg(null);
    try {
      const res = await archGuardApi.uploadRegulationPdf(file, pdfCategory);
      alert(res.message);
    } catch (err: unknown) {
      setErrorMsg(`PDF Yükleme Hatası: ${getErrorMessage(err)}`);
    } finally {
      setIsPdfUploading(false);
      e.target.value = '';
    }
  };

  const handleRagSearch = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!ragQuery.trim()) return;
    try {
      const results = await archGuardApi.searchRegulations(ragQuery);
      setRagResults(results);
    } catch (err: unknown) {
      setErrorMsg(getErrorMessage(err));
    }
  };

  const filteredFindings = findings.filter((f) =>
    severityFilter === 'ALL' ? true : f.severity === severityFilter
  );

  const errorCount = findings.filter((f) => f.severity === 'Error').length;
  const warningCount = findings.filter((f) => f.severity === 'Warning').length;
  const passedCount = findings.filter((f) => f.severity === 'Passed').length;

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 flex flex-col md:flex-row font-sans">
      {/* SOL SIDEBAR: PROJELER & IFC YÜKLEME */}
      <aside className="w-full md:w-80 bg-slate-900 border-b md:border-b-0 md:border-r border-slate-800 p-5 flex flex-col justify-between">
        <div>
          <div className="flex items-center gap-3 mb-6">
            <div className="p-2.5 bg-indigo-600 rounded-xl shadow-lg shadow-indigo-500/20">
              <Building2 className="w-6 h-6 text-white" />
            </div>
            <div>
              <h1 className="text-lg font-bold tracking-tight text-white">ArchGuard AI</h1>
              <p className="text-xs text-slate-400">BIM & Yönetmelik Otomasyonu</p>
            </div>
          </div>

          <label className="w-full flex items-center justify-center gap-2 px-4 py-3 bg-indigo-600 hover:bg-indigo-500 transition rounded-xl cursor-pointer font-medium text-sm shadow-md mb-6">
            {isUploading ? <Loader2 className="w-4 h-4 animate-spin" /> : <Upload className="w-4 h-4" />}
            <span>{isUploading ? 'IFC Ayrıştırılıyor...' : 'Yeni .IFC Modeli Yükle'}</span>
            <input type="file" accept=".ifc" className="hidden" onChange={handleIfcUpload} disabled={isUploading} />
          </label>

          <div className="text-xs font-semibold uppercase tracking-wider text-slate-400 mb-3">
            Kayıtlı BIM Modelleri ({projects.length})
          </div>

          <div className="space-y-2 max-h-[55vh] overflow-y-auto pr-1">
            {projects.map((proj) => {
              const isSelected = selectedProject?.projectId === proj.projectId;
              return (
                <button
                  key={proj.projectId}
                  onClick={() => setSelectedProject(proj)}
                  className={`w-full text-left p-3.5 rounded-xl border transition ${
                    isSelected
                      ? 'bg-indigo-950/60 border-indigo-500/60 text-white'
                      : 'bg-slate-800/40 border-slate-800 text-slate-300 hover:bg-slate-800'
                  }`}
                >
                  <div className="flex items-center justify-between mb-1">
                    <span className="font-semibold text-sm truncate pr-2">{proj.projectName}</span>
                    <span
                      className={`text-[10px] px-2 py-0.5 rounded-full font-medium ${
                        proj.sourceType === 'RevitPlugin'
                          ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/30'
                          : 'bg-sky-500/20 text-sky-300 border border-sky-500/30'
                      }`}
                    >
                      {proj.sourceType}
                    </span>
                  </div>
                  <div className="text-xs text-slate-400 truncate">{proj.sourceFileName}</div>
                  <div className="text-[11px] text-slate-500 mt-2 flex gap-3">
                    <span>{proj.storeyCount} Kat</span>
                    <span>•</span>
                    <span>{proj.doorCount} Kapı</span>
                    <span>•</span>
                    <span>{proj.wallCount} Duvar</span>
                  </div>
                </button>
              );
            })}
          </div>
        </div>

        <div className="pt-4 border-t border-slate-800 text-xs text-slate-500">
          Hybrid Pipeline: IFC + Revit Plugin
        </div>
      </aside>

      {/* ANA İÇERİK ALANI */}
      <main className="flex-1 p-6 md:p-8 overflow-y-auto">
        {errorMsg && (
          <div className="mb-6 p-4 bg-rose-950/70 border border-rose-500/50 rounded-xl text-rose-200 text-sm flex items-center justify-between">
            <span>{errorMsg}</span>
            <button onClick={() => setErrorMsg(null)} className="text-xs underline ml-4">Kapat</button>
          </div>
        )}

        {!selectedProject ? (
          <div className="h-full flex flex-col items-center justify-center text-slate-400">
            <Building2 className="w-14 h-14 mb-4 text-slate-600" />
            <p className="text-lg font-medium">Başlamak için sol menüden bir .IFC dosyası yükleyin.</p>
          </div>
        ) : (
          <div className="max-w-6xl mx-auto space-y-6">
            {/* ÜST BAŞLIK & DİNAMİK LLM SEÇİCİ */}
            <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5 flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
              <div>
                <div className="flex items-center gap-2.5">
                  <h2 className="text-xl font-bold text-white">{selectedProject.projectName}</h2>
                  <span className="text-xs px-2.5 py-0.5 rounded-md bg-slate-800 text-slate-300 border border-slate-700">
                    {selectedProject.sourceFileName}
                  </span>
                </div>
                <p className="text-xs text-slate-400 mt-1">Proje ID: {selectedProject.projectId}</p>
              </div>

              <div className="flex flex-wrap items-center gap-3">
                {/* LLM Provider Toggle */}
                <div className="flex items-center bg-slate-950 p-1 rounded-xl border border-slate-800">
                  <button
                    onClick={() => setProvider(1)}
                    className={`flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium transition ${
                      provider === 1 ? 'bg-indigo-600 text-white' : 'text-slate-400 hover:text-white'
                    }`}
                  >
                    <Cpu className="w-3.5 h-3.5" />
                    Google Gemini
                  </button>
                  <button
                    onClick={() => setProvider(2)}
                    className={`flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium transition ${
                      provider === 2 ? 'bg-emerald-600 text-white' : 'text-slate-400 hover:text-white'
                    }`}
                  >
                    <Cpu className="w-3.5 h-3.5" />
                    NVIDIA NIM (8B)
                  </button>
                </div>

                <button
                  onClick={handleRunAudit}
                  disabled={isAuditing}
                  className="flex items-center gap-2 px-4 py-2.5 bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 transition rounded-xl text-sm font-semibold text-white shadow-lg shadow-emerald-600/20"
                >
                  {isAuditing ? <Loader2 className="w-4 h-4 animate-spin" /> : <ShieldCheck className="w-4 h-4" />}
                  <span>{isAuditing ? 'AI Denetliyor...' : 'Yönetmelik Denetimini Başlat'}</span>
                </button>
              </div>
            </div>

            {/* METRAJ ÖZET KARTLARI */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
              <div className="bg-slate-900 border border-slate-800 p-4 rounded-2xl">
                <div className="flex items-center justify-between text-slate-400 text-xs mb-2">
                  <span>Kat & Mahal</span>
                  <Layers className="w-4 h-4 text-indigo-400" />
                </div>
                <div className="text-2xl font-bold text-white">
                  {selectedProject.storeyCount} <span className="text-sm font-normal text-slate-400">Kat</span> / {selectedProject.spaceCount} <span className="text-sm font-normal text-slate-400">Oda</span>
                </div>
              </div>

              <div className="bg-slate-900 border border-slate-800 p-4 rounded-2xl">
                <div className="flex items-center justify-between text-slate-400 text-xs mb-2">
                  <span>Toplam Kapı</span>
                  <DoorOpen className="w-4 h-4 text-amber-400" />
                </div>
                <div className="text-2xl font-bold text-white">{selectedProject.doorCount} <span className="text-sm font-normal text-slate-400">Adet</span></div>
              </div>

              <div className="bg-slate-900 border border-slate-800 p-4 rounded-2xl">
                <div className="flex items-center justify-between text-slate-400 text-xs mb-2">
                  <span>Duvar Alanı Metrajı</span>
                  <Box className="w-4 h-4 text-sky-400" />
                </div>
                <div className="text-2xl font-bold text-white">{selectedProject.totalWallAreaM2} <span className="text-sm font-normal text-slate-400">m²</span></div>
              </div>

              <div className="bg-slate-900 border border-slate-800 p-4 rounded-2xl">
                <div className="flex items-center justify-between text-slate-400 text-xs mb-2">
                  <span>Duvar Hacmi Metrajı</span>
                  <Box className="w-4 h-4 text-emerald-400" />
                </div>
                <div className="text-2xl font-bold text-white">{selectedProject.totalWallVolumeM3} <span className="text-sm font-normal text-slate-400">m³</span></div>
              </div>
            </div>

            {/* SEKMELER */}
            <div className="flex gap-2 border-b border-slate-800 pb-2">
              <button
                onClick={() => setActiveTab('audit')}
                className={`flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-medium transition ${
                  activeTab === 'audit' ? 'bg-slate-800 text-white' : 'text-slate-400 hover:text-white'
                }`}
              >
                <ShieldCheck className="w-4 h-4" />
                Denetim Bulguları ({findings.length})
              </button>
              <button
                onClick={() => setActiveTab('chat')}
                className={`flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-medium transition ${
                  activeTab === 'chat' ? 'bg-slate-800 text-white' : 'text-slate-400 hover:text-white'
                }`}
              >
                <Bot className="w-4 h-4" />
                BIM Asistanı (Tool Calling)
              </button>
              <button
                onClick={() => setActiveTab('rag')}
                className={`flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-medium transition ${
                  activeTab === 'rag' ? 'bg-slate-800 text-white' : 'text-slate-400 hover:text-white'
                }`}
              >
                <FileText className="w-4 h-4" />
                Yönetmelik PDF & Qdrant RAG
              </button>
            </div>

            {/* SEKME 1: DENETİM RAPORU */}
            {activeTab === 'audit' && (
              <div className="space-y-4">
                <div className="flex flex-wrap gap-2">
                  <button
                    onClick={() => setSeverityFilter('ALL')}
                    className={`px-3 py-1.5 rounded-lg text-xs font-medium border ${
                      severityFilter === 'ALL' ? 'bg-slate-800 border-slate-600 text-white' : 'border-slate-800 text-slate-400'
                    }`}
                  >
                    Tümü ({findings.length})
                  </button>
                  <button
                    onClick={() => setSeverityFilter('Error')}
                    className={`px-3 py-1.5 rounded-lg text-xs font-medium border ${
                      severityFilter === 'Error' ? 'bg-rose-950 border-rose-500 text-rose-200' : 'border-slate-800 text-rose-400'
                    }`}
                  >
                    Kritik İhlal ({errorCount})
                  </button>
                  <button
                    onClick={() => setSeverityFilter('Warning')}
                    className={`px-3 py-1.5 rounded-lg text-xs font-medium border ${
                      severityFilter === 'Warning' ? 'bg-amber-950 border-amber-500 text-amber-200' : 'border-slate-800 text-amber-400'
                    }`}
                  >
                    Uyarı ({warningCount})
                  </button>
                  <button
                    onClick={() => setSeverityFilter('Passed')}
                    className={`px-3 py-1.5 rounded-lg text-xs font-medium border ${
                      severityFilter === 'Passed' ? 'bg-emerald-950 border-emerald-500 text-emerald-200' : 'border-slate-800 text-emerald-400'
                    }`}
                  >
                    Uygun ({passedCount})
                  </button>
                </div>

                {filteredFindings.length === 0 ? (
                  <div className="bg-slate-900 border border-slate-800 rounded-2xl p-10 text-center text-slate-400 text-sm">
                    Bu filtreye uygun bulgu bulunamadı. Sağ üstten <strong>"Yönetmelik Denetimini Başlat"</strong> butonuna basarak AI denetimini çalıştırabilirsiniz.
                  </div>
                ) : (
                  <div className="space-y-3">
                    {filteredFindings.map((item) => (
                      <div key={item.id} className="bg-slate-900 border border-slate-800 rounded-2xl p-5">
                        <div className="flex flex-wrap items-center justify-between gap-2 mb-2">
                          <div className="flex items-center gap-2.5">
                            {item.severity === 'Error' && <XCircle className="w-5 h-5 text-rose-500" />}
                            {item.severity === 'Warning' && <AlertTriangle className="w-5 h-5 text-amber-400" />}
                            {item.severity === 'Passed' && <CheckCircle2 className="w-5 h-5 text-emerald-400" />}
                            <span className="font-semibold text-white">{item.elementName}</span>
                            <span className="text-xs px-2 py-0.5 rounded bg-slate-800 text-slate-300">
                              {item.elementType} • {item.storeyName}
                            </span>
                          </div>
                          <div className="flex items-center gap-2">
                            <span className="text-xs px-2.5 py-1 rounded-lg bg-indigo-950 text-indigo-300 border border-indigo-800/60">
                              {item.ruleReference}
                            </span>
                            <span className="text-[11px] text-slate-500">{item.llmProviderUsed}</span>
                          </div>
                        </div>
                        <p className="text-sm text-slate-300 mb-3">{item.description}</p>
                        <div className="bg-slate-950/70 border border-slate-800/80 rounded-xl p-3 text-xs text-slate-300">
                          <strong className="text-emerald-400">Çözüm Önerisi:</strong> {item.suggestedFix}
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}

            {/* SEKME 2: BIM ASİSTANI (TOOL CALLING CHAT) */}
            {activeTab === 'chat' && (
              <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5 flex flex-col h-[500px]">
                <div className="flex-1 overflow-y-auto space-y-4 pr-2 mb-4">
                  {chatHistory.map((msg, idx) => (
                    <div key={idx} className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                      <div
                        className={`max-w-[80%] rounded-2xl p-4 text-sm whitespace-pre-wrap ${
                          msg.role === 'user'
                            ? 'bg-indigo-600 text-white'
                            : 'bg-slate-800 text-slate-200 border border-slate-700'
                        }`}
                      >
                        {msg.provider && (
                          <div className="text-[10px] uppercase tracking-wider text-indigo-300 mb-1 font-semibold">
                            {msg.provider} • BIM Tool Calling
                          </div>
                        )}
                        {msg.text}
                      </div>
                    </div>
                  ))}
                  {isChatting && (
                    <div className="flex items-center gap-2 text-xs text-slate-400">
                      <Loader2 className="w-4 h-4 animate-spin" />
                      <span>Model veritabanı araçları (BimModelTools) sorgulanıyor...</span>
                    </div>
                  )}
                </div>

                <form onSubmit={handleSendChat} className="flex gap-2">
                  <input
                    type="text"
                    value={chatInput}
                    onChange={(e) => setChatInput(e.target.value)}
                    placeholder="Örn: Dış duvarların toplam alanı ve hacmi nedir? Veya hangi kapılar 90 cm altında?"
                    className="flex-1 bg-slate-950 border border-slate-800 rounded-xl px-4 py-3 text-sm text-white focus:outline-none focus:border-indigo-500"
                  />
                  <button
                    type="submit"
                    disabled={isChatting || !chatInput.trim()}
                    className="px-5 py-3 bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 rounded-xl text-sm font-medium flex items-center gap-2"
                  >
                    <Send className="w-4 h-4" />
                    Gönder
                  </button>
                </form>
              </div>
            )}

            {/* SEKME 3: YÖNETMELİK PDF & QDRANT RAG */}
            {activeTab === 'rag' && (
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5 space-y-4">
                  <h3 className="font-semibold text-white">Yeni Yönetmelik PDF İndeksle</h3>
                  <p className="text-xs text-slate-400">
                    Planlı Alanlar İmar Yönetmeliği, Yangın Yönetmeliği veya şirket içi BIM standart PDF dosyalarını Qdrant vektör veritabanına gömün.
                  </p>
                  <div>
                    <label className="block text-xs text-slate-400 mb-1">Yönetmelik Kategorisi</label>
                    <input
                      type="text"
                      value={pdfCategory}
                      onChange={(e) => setPdfCategory(e.target.value)}
                      className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-sm text-white"
                    />
                  </div>
                  <label className="w-full flex items-center justify-center gap-2 px-4 py-3 bg-slate-800 hover:bg-slate-700 border border-slate-700 rounded-xl cursor-pointer text-sm font-medium">
                    {isPdfUploading ? <Loader2 className="w-4 h-4 animate-spin" /> : <Upload className="w-4 h-4" />}
                    <span>{isPdfUploading ? 'PDF Parçalanıp Vektörleştiriliyor...' : 'PDF Dosyası Seç ve İndeksle'}</span>
                    <input type="file" accept=".pdf" className="hidden" onChange={handlePdfUpload} disabled={isPdfUploading} />
                  </label>
                </div>

                <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5 space-y-4">
                  <h3 className="font-semibold text-white">Qdrant Anlamsal Kural Arama</h3>
                  <form onSubmit={handleRagSearch} className="flex gap-2">
                    <input
                      type="text"
                      value={ragQuery}
                      onChange={(e) => setRagQuery(e.target.value)}
                      placeholder="Örn: yangın kaçış kapısı genişliği..."
                      className="flex-1 bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-sm text-white"
                    />
                    <button type="submit" className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 rounded-xl text-sm flex items-center gap-1">
                      <Search className="w-4 h-4" />
                      Ara
                    </button>
                  </form>

                  <div className="space-y-2 max-h-64 overflow-y-auto">
                    {ragResults.map((r, i) => (
                      <div key={i} className="bg-slate-950 border border-slate-800 p-3 rounded-xl text-xs">
                        <div className="flex justify-between font-semibold text-indigo-300 mb-1">
                          <span>{r.ruleCode}</span>
                          <span>Skor: {(r.score * 100).toFixed(1)}%</span>
                        </div>
                        <p className="text-slate-300">{r.content}</p>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            )}
          </div>
        )}
      </main>
    </div>
  );
}

export default App;


// import React, { useCallback, useEffect, useState } from 'react';
// import {
//   Building2,
//   Upload,
//   ShieldCheck,
//   AlertTriangle,
//   XCircle,
//   CheckCircle2,
//   Bot,
//   Send,
//   FileText,
//   Layers,
//   DoorOpen,
//   Box,
//   Cpu,
//   Search,
//   Loader2
// } from 'lucide-react';
// import { archGuardApi } from './api/archGuardApi';
// import type {
//   AuditFinding,
//   LlmProvider,
//   ProjectSummary,
//   RegulationSearchResult
// } from './api/archGuardApi';

// function getErrorMessage(err: unknown): string {
//   if (err instanceof Error) return err.message;
//   return String(err);
// }

// export function App() {
//   const [projects, setProjects] = useState<ProjectSummary[]>([]);
//   const [selectedProject, setSelectedProject] = useState<ProjectSummary | null>(null);
//   const [provider, setProvider] = useState<LlmProvider>(1); // 1: Gemini, 2: Nvidia
//   const [activeTab, setActiveTab] = useState<'audit' | 'chat' | 'rag'>('audit');

//   // States for Data
//   const [findings, setFindings] = useState<AuditFinding[]>([]);
//   const [severityFilter, setSeverityFilter] = useState<'ALL' | 'Error' | 'Warning' | 'Passed'>('ALL');
//   const [isUploading, setIsUploading] = useState(false);
//   const [isAuditing, setIsAuditing] = useState(false);
//   const [errorMsg, setErrorMsg] = useState<string | null>(null);

//   // Chat States
//   const [chatHistory, setChatHistory] = useState<{ role: 'user' | 'ai'; text: string; provider?: string }[]>([]);
//   const [chatInput, setChatInput] = useState('');
//   const [isChatting, setIsChatting] = useState(false);

//   // RAG States
//   const [ragQuery, setRagQuery] = useState('');
//   const [ragResults, setRagResults] = useState<RegulationSearchResult[]>([]);
//   const [pdfCategory, setPdfCategory] = useState('İmar ve Yangın Yönetmeliği');
//   const [isPdfUploading, setIsPdfUploading] = useState(false);

//   const loadProjects = useCallback(async () => {
//     try {
//       const list = await archGuardApi.getProjects();
//       setProjects((prevProjects) => {
//         // Eğer proje sayısı veya içerikleri değiştiyse state'i güncelle
//         if (JSON.stringify(prevProjects) !== JSON.stringify(list)) {
//           return list;
//         }
//         return prevProjects;
//       });
      
//       setSelectedProject((prev) => {
//         if (!prev && list.length > 0) return list[0];
//         // Eğer aktif seçili proje listede hala varsa, güncel verilerini koru
//         if (prev) {
//           const updatedCurrent = list.find((p) => p.projectId === prev.projectId);
//           return updatedCurrent ?? prev;
//         }
//         return prev;
//       });
//     } catch (err: unknown) {
//       setErrorMsg(getErrorMessage(err));
//     }
//   }, []);

//   // İlk yükleme ve arka planda otomatik yenileme (Polling - 4 saniyede bir)
//   useEffect(() => {
//     void loadProjects();
//     const intervalId = setInterval(() => {
//       void loadProjects();
//     }, 4000);

//     return () => clearInterval(intervalId);
//   }, [loadProjects]);

//   useEffect(() => {
//     if (selectedProject) {
//       void loadFindings(selectedProject.projectId);
//       setChatHistory([
//         {
//           role: 'ai',
//           text: `"${selectedProject.projectName}" modeli yüklendi (${selectedProject.doorCount} kapı, ${selectedProject.wallCount} duvar, ${selectedProject.spaceCount} mahal). Metraj veya yönetmelik uyumluluğu hakkında soru sorabilirsiniz.`
//         }
//       ]);
//     }
//   }, [selectedProject]);

//   const loadFindings = async (projectId: string) => {
//     try {
//       const data = await archGuardApi.getFindings(projectId);
//       setFindings(data);
//     } catch {
//       setFindings([]);
//     }
//   };

//   const handleIfcUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
//     const file = e.target.files?.[0];
//     if (!file) return;
//     setIsUploading(true);
//     setErrorMsg(null);
//     try {
//       const created = await archGuardApi.uploadIfc(file);
//       setProjects((prev) => [created, ...prev]);
//       setSelectedProject(created);
//     } catch (err: unknown) {
//       setErrorMsg(`IFC Yükleme Hatası: ${getErrorMessage(err)}`);
//     } finally {
//       setIsUploading(false);
//       e.target.value = '';
//     }
//   };

//   const handleRunAudit = async () => {
//     if (!selectedProject) return;
//     setIsAuditing(true);
//     setErrorMsg(null);
//     try {
//       const result = await archGuardApi.runAudit(selectedProject.projectId, provider);
//       setFindings(result);
//       setActiveTab('audit');
//     } catch (err: unknown) {
//       setErrorMsg(`Denetim Hatası: ${getErrorMessage(err)}`);
//     } finally {
//       setIsAuditing(false);
//     }
//   };

//   const handleSendChat = async (e: React.FormEvent) => {
//     e.preventDefault();
//     if (!selectedProject || !chatInput.trim() || isChatting) return;

//     const userText = chatInput.trim();
//     setChatInput('');
//     setChatHistory((prev) => [...prev, { role: 'user', text: userText }]);
//     setIsChatting(true);

//     try {
//       const res = await archGuardApi.chatWithModel(selectedProject.projectId, userText, provider);
//       setChatHistory((prev) => [...prev, { role: 'ai', text: res.reply, provider: res.providerUsed }]);
//     } catch (err: unknown) {
//       setChatHistory((prev) => [...prev, { role: 'ai', text: `Hata: ${getErrorMessage(err)}` }]);
//     } finally {
//       setIsChatting(false);
//     }
//   };

//   const handlePdfUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
//     const file = e.target.files?.[0];
//     if (!file) return;
//     setIsPdfUploading(true);
//     setErrorMsg(null);
//     try {
//       const res = await archGuardApi.uploadRegulationPdf(file, pdfCategory);
//       alert(res.message);
//     } catch (err: unknown) {
//       setErrorMsg(`PDF Yükleme Hatası: ${getErrorMessage(err)}`);
//     } finally {
//       setIsPdfUploading(false);
//       e.target.value = '';
//     }
//   };

//   const handleRagSearch = async (e: React.FormEvent) => {
//     e.preventDefault();
//     if (!ragQuery.trim()) return;
//     try {
//       const results = await archGuardApi.searchRegulations(ragQuery);
//       setRagResults(results);
//     } catch (err: unknown) {
//       setErrorMsg(getErrorMessage(err));
//     }
//   };

//   const filteredFindings = findings.filter((f) =>
//     severityFilter === 'ALL' ? true : f.severity === severityFilter
//   );

//   const errorCount = findings.filter((f) => f.severity === 'Error').length;
//   const warningCount = findings.filter((f) => f.severity === 'Warning').length;
//   const passedCount = findings.filter((f) => f.severity === 'Passed').length;

//   return (
//     <div className="min-h-screen bg-slate-950 text-slate-100 flex flex-col md:flex-row font-sans">
//       {/* SOL SIDEBAR: PROJELER & IFC YÜKLEME */}
//       <aside className="w-full md:w-80 bg-slate-900 border-b md:border-b-0 md:border-r border-slate-800 p-5 flex flex-col justify-between">
//         <div>
//           <div className="flex items-center gap-3 mb-6">
//             <div className="p-2.5 bg-indigo-600 rounded-xl shadow-lg shadow-indigo-500/20">
//               <Building2 className="w-6 h-6 text-white" />
//             </div>
//             <div>
//               <h1 className="text-lg font-bold tracking-tight text-white">ArchGuard AI</h1>
//               <p className="text-xs text-slate-400">BIM & Yönetmelik Otomasyonu</p>
//             </div>
//           </div>

//           <label className="w-full flex items-center justify-center gap-2 px-4 py-3 bg-indigo-600 hover:bg-indigo-500 transition rounded-xl cursor-pointer font-medium text-sm shadow-md mb-6">
//             {isUploading ? <Loader2 className="w-4 h-4 animate-spin" /> : <Upload className="w-4 h-4" />}
//             <span>{isUploading ? 'IFC Ayrıştırılıyor...' : 'Yeni .IFC Modeli Yükle'}</span>
//             <input type="file" accept=".ifc" className="hidden" onChange={handleIfcUpload} disabled={isUploading} />
//           </label>

//           <div className="text-xs font-semibold uppercase tracking-wider text-slate-400 mb-3">
//             Kayıtlı BIM Modelleri ({projects.length})
//           </div>

//           <div className="space-y-2 max-h-[55vh] overflow-y-auto pr-1">
//             {projects.map((proj) => {
//               const isSelected = selectedProject?.projectId === proj.projectId;
//               return (
//                 <button
//                   key={proj.projectId}
//                   onClick={() => setSelectedProject(proj)}
//                   className={`w-full text-left p-3.5 rounded-xl border transition ${
//                     isSelected
//                       ? 'bg-indigo-950/60 border-indigo-500/60 text-white'
//                       : 'bg-slate-800/40 border-slate-800 text-slate-300 hover:bg-slate-800'
//                   }`}
//                 >
//                   <div className="flex items-center justify-between mb-1">
//                     <span className="font-semibold text-sm truncate pr-2">{proj.projectName}</span>
//                     <span
//                       className={`text-[10px] px-2 py-0.5 rounded-full font-medium ${
//                         proj.sourceType === 'RevitPlugin'
//                           ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/30'
//                           : 'bg-sky-500/20 text-sky-300 border border-sky-500/30'
//                       }`}
//                     >
//                       {proj.sourceType}
//                     </span>
//                   </div>
//                   <div className="text-xs text-slate-400 truncate">{proj.sourceFileName}</div>
//                   <div className="text-[11px] text-slate-500 mt-2 flex gap-3">
//                     <span>{proj.storeyCount} Kat</span>
//                     <span>•</span>
//                     <span>{proj.doorCount} Kapı</span>
//                     <span>•</span>
//                     <span>{proj.wallCount} Duvar</span>
//                   </div>
//                 </button>
//               );
//             })}
//           </div>
//         </div>

//         <div className="pt-4 border-t border-slate-800 text-xs text-slate-500">
//           Hybrid Pipeline: IFC + Revit Plugin
//         </div>
//       </aside>

//       {/* ANA İÇERİK ALANI */}
//       <main className="flex-1 p-6 md:p-8 overflow-y-auto">
//         {errorMsg && (
//           <div className="mb-6 p-4 bg-rose-950/70 border border-rose-500/50 rounded-xl text-rose-200 text-sm flex items-center justify-between">
//             <span>{errorMsg}</span>
//             <button onClick={() => setErrorMsg(null)} className="text-xs underline ml-4">Kapat</button>
//           </div>
//         )}

//         {!selectedProject ? (
//           <div className="h-full flex flex-col items-center justify-center text-slate-400">
//             <Building2 className="w-14 h-14 mb-4 text-slate-600" />
//             <p className="text-lg font-medium">Başlamak için sol menüden bir .IFC dosyası yükleyin.</p>
//           </div>
//         ) : (
//           <div className="max-w-6xl mx-auto space-y-6">
//             {/* ÜST BAŞLIK & DİNAMİK LLM SEÇİCİ */}
//             <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5 flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
//               <div>
//                 <div className="flex items-center gap-2.5">
//                   <h2 className="text-xl font-bold text-white">{selectedProject.projectName}</h2>
//                   <span className="text-xs px-2.5 py-0.5 rounded-md bg-slate-800 text-slate-300 border border-slate-700">
//                     {selectedProject.sourceFileName}
//                   </span>
//                 </div>
//                 <p className="text-xs text-slate-400 mt-1">Proje ID: {selectedProject.projectId}</p>
//               </div>

//               <div className="flex flex-wrap items-center gap-3">
//                 {/* LLM Provider Toggle */}
//                 <div className="flex items-center bg-slate-950 p-1 rounded-xl border border-slate-800">
//                   <button
//                     onClick={() => setProvider(1)}
//                     className={`flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium transition ${
//                       provider === 1 ? 'bg-indigo-600 text-white' : 'text-slate-400 hover:text-white'
//                     }`}
//                   >
//                     <Cpu className="w-3.5 h-3.5" />
//                     Google Gemini
//                   </button>
//                   <button
//                     onClick={() => setProvider(2)}
//                     className={`flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium transition ${
//                       provider === 2 ? 'bg-emerald-600 text-white' : 'text-slate-400 hover:text-white'
//                     }`}
//                   >
//                     <Cpu className="w-3.5 h-3.5" />
//                     NVIDIA NIM (8B)
//                   </button>
//                 </div>

//                 <button
//                   onClick={handleRunAudit}
//                   disabled={isAuditing}
//                   className="flex items-center gap-2 px-4 py-2.5 bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 transition rounded-xl text-sm font-semibold text-white shadow-lg shadow-emerald-600/20"
//                 >
//                   {isAuditing ? <Loader2 className="w-4 h-4 animate-spin" /> : <ShieldCheck className="w-4 h-4" />}
//                   <span>{isAuditing ? 'AI Denetliyor...' : 'Yönetmelik Denetimini Başlat'}</span>
//                 </button>
//               </div>
//             </div>

//             {/* METRAJ ÖZET KARTLARI */}
//             <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
//               <div className="bg-slate-900 border border-slate-800 p-4 rounded-2xl">
//                 <div className="flex items-center justify-between text-slate-400 text-xs mb-2">
//                   <span>Kat & Mahal</span>
//                   <Layers className="w-4 h-4 text-indigo-400" />
//                 </div>
//                 <div className="text-2xl font-bold text-white">
//                   {selectedProject.storeyCount} <span className="text-sm font-normal text-slate-400">Kat</span> / {selectedProject.spaceCount} <span className="text-sm font-normal text-slate-400">Oda</span>
//                 </div>
//               </div>

//               <div className="bg-slate-900 border border-slate-800 p-4 rounded-2xl">
//                 <div className="flex items-center justify-between text-slate-400 text-xs mb-2">
//                   <span>Toplam Kapı</span>
//                   <DoorOpen className="w-4 h-4 text-amber-400" />
//                 </div>
//                 <div className="text-2xl font-bold text-white">{selectedProject.doorCount} <span className="text-sm font-normal text-slate-400">Adet</span></div>
//               </div>

//               <div className="bg-slate-900 border border-slate-800 p-4 rounded-2xl">
//                 <div className="flex items-center justify-between text-slate-400 text-xs mb-2">
//                   <span>Duvar Alanı Metrajı</span>
//                   <Box className="w-4 h-4 text-sky-400" />
//                 </div>
//                 <div className="text-2xl font-bold text-white">{selectedProject.totalWallAreaM2} <span className="text-sm font-normal text-slate-400">m²</span></div>
//               </div>

//               <div className="bg-slate-900 border border-slate-800 p-4 rounded-2xl">
//                 <div className="flex items-center justify-between text-slate-400 text-xs mb-2">
//                   <span>Duvar Hacmi Metrajı</span>
//                   <Box className="w-4 h-4 text-emerald-400" />
//                 </div>
//                 <div className="text-2xl font-bold text-white">{selectedProject.totalWallVolumeM3} <span className="text-sm font-normal text-slate-400">m³</span></div>
//               </div>
//             </div>

//             {/* SEKMELER */}
//             <div className="flex gap-2 border-b border-slate-800 pb-2">
//               <button
//                 onClick={() => setActiveTab('audit')}
//                 className={`flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-medium transition ${
//                   activeTab === 'audit' ? 'bg-slate-800 text-white' : 'text-slate-400 hover:text-white'
//                 }`}
//               >
//                 <ShieldCheck className="w-4 h-4" />
//                 Denetim Bulguları ({findings.length})
//               </button>
//               <button
//                 onClick={() => setActiveTab('chat')}
//                 className={`flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-medium transition ${
//                   activeTab === 'chat' ? 'bg-slate-800 text-white' : 'text-slate-400 hover:text-white'
//                 }`}
//               >
//                 <Bot className="w-4 h-4" />
//                 BIM Asistanı (Tool Calling)
//               </button>
//               <button
//                 onClick={() => setActiveTab('rag')}
//                 className={`flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-medium transition ${
//                   activeTab === 'rag' ? 'bg-slate-800 text-white' : 'text-slate-400 hover:text-white'
//                 }`}
//               >
//                 <FileText className="w-4 h-4" />
//                 Yönetmelik PDF & Qdrant RAG
//               </button>
//             </div>

//             {/* SEKME 1: DENETİM RAPORU */}
//             {activeTab === 'audit' && (
//               <div className="space-y-4">
//                 <div className="flex flex-wrap gap-2">
//                   <button
//                     onClick={() => setSeverityFilter('ALL')}
//                     className={`px-3 py-1.5 rounded-lg text-xs font-medium border ${
//                       severityFilter === 'ALL' ? 'bg-slate-800 border-slate-600 text-white' : 'border-slate-800 text-slate-400'
//                     }`}
//                   >
//                     Tümü ({findings.length})
//                   </button>
//                   <button
//                     onClick={() => setSeverityFilter('Error')}
//                     className={`px-3 py-1.5 rounded-lg text-xs font-medium border ${
//                       severityFilter === 'Error' ? 'bg-rose-950 border-rose-500 text-rose-200' : 'border-slate-800 text-rose-400'
//                     }`}
//                   >
//                     Kritik İhlal ({errorCount})
//                   </button>
//                   <button
//                     onClick={() => setSeverityFilter('Warning')}
//                     className={`px-3 py-1.5 rounded-lg text-xs font-medium border ${
//                       severityFilter === 'Warning' ? 'bg-amber-950 border-amber-500 text-amber-200' : 'border-slate-800 text-amber-400'
//                     }`}
//                   >
//                     Uyarı ({warningCount})
//                   </button>
//                   <button
//                     onClick={() => setSeverityFilter('Passed')}
//                     className={`px-3 py-1.5 rounded-lg text-xs font-medium border ${
//                       severityFilter === 'Passed' ? 'bg-emerald-950 border-emerald-500 text-emerald-200' : 'border-slate-800 text-emerald-400'
//                     }`}
//                   >
//                     Uygun ({passedCount})
//                   </button>
//                 </div>

//                 {filteredFindings.length === 0 ? (
//                   <div className="bg-slate-900 border border-slate-800 rounded-2xl p-10 text-center text-slate-400 text-sm">
//                     Bu filtreye uygun bulgu bulunamadı. Sağ üstten <strong>"Yönetmelik Denetimini Başlat"</strong> butonuna basarak AI denetimini çalıştırabilirsiniz.
//                   </div>
//                 ) : (
//                   <div className="space-y-3">
//                     {filteredFindings.map((item) => (
//                       <div key={item.id} className="bg-slate-900 border border-slate-800 rounded-2xl p-5">
//                         <div className="flex flex-wrap items-center justify-between gap-2 mb-2">
//                           <div className="flex items-center gap-2.5">
//                             {item.severity === 'Error' && <XCircle className="w-5 h-5 text-rose-500" />}
//                             {item.severity === 'Warning' && <AlertTriangle className="w-5 h-5 text-amber-400" />}
//                             {item.severity === 'Passed' && <CheckCircle2 className="w-5 h-5 text-emerald-400" />}
//                             <span className="font-semibold text-white">{item.elementName}</span>
//                             <span className="text-xs px-2 py-0.5 rounded bg-slate-800 text-slate-300">
//                               {item.elementType} • {item.storeyName}
//                             </span>
//                           </div>
//                           <div className="flex items-center gap-2">
//                             <span className="text-xs px-2.5 py-1 rounded-lg bg-indigo-950 text-indigo-300 border border-indigo-800/60">
//                               {item.ruleReference}
//                             </span>
//                             <span className="text-[11px] text-slate-500">{item.llmProviderUsed}</span>
//                           </div>
//                         </div>
//                         <p className="text-sm text-slate-300 mb-3">{item.description}</p>
//                         <div className="bg-slate-950/70 border border-slate-800/80 rounded-xl p-3 text-xs text-slate-300">
//                           <strong className="text-emerald-400">Çözüm Önerisi:</strong> {item.suggestedFix}
//                         </div>
//                       </div>
//                     ))}
//                   </div>
//                 )}
//               </div>
//             )}

//             {/* SEKME 2: BIM ASİSTANI (TOOL CALLING CHAT) */}
//             {activeTab === 'chat' && (
//               <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5 flex flex-col h-[500px]">
//                 <div className="flex-1 overflow-y-auto space-y-4 pr-2 mb-4">
//                   {chatHistory.map((msg, idx) => (
//                     <div key={idx} className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
//                       <div
//                         className={`max-w-[80%] rounded-2xl p-4 text-sm whitespace-pre-wrap ${
//                           msg.role === 'user'
//                             ? 'bg-indigo-600 text-white'
//                             : 'bg-slate-800 text-slate-200 border border-slate-700'
//                         }`}
//                       >
//                         {msg.provider && (
//                           <div className="text-[10px] uppercase tracking-wider text-indigo-300 mb-1 font-semibold">
//                             {msg.provider} • BIM Tool Calling
//                           </div>
//                         )}
//                         {msg.text}
//                       </div>
//                     </div>
//                   ))}
//                   {isChatting && (
//                     <div className="flex items-center gap-2 text-xs text-slate-400">
//                       <Loader2 className="w-4 h-4 animate-spin" />
//                       <span>Model veritabanı araçları (BimModelTools) sorgulanıyor...</span>
//                     </div>
//                   )}
//                 </div>

//                 <form onSubmit={handleSendChat} className="flex gap-2">
//                   <input
//                     type="text"
//                     value={chatInput}
//                     onChange={(e) => setChatInput(e.target.value)}
//                     placeholder="Örn: Dış duvarların toplam alanı ve hacmi nedir? Veya hangi kapılar 90 cm altında?"
//                     className="flex-1 bg-slate-950 border border-slate-800 rounded-xl px-4 py-3 text-sm text-white focus:outline-none focus:border-indigo-500"
//                   />
//                   <button
//                     type="submit"
//                     disabled={isChatting || !chatInput.trim()}
//                     className="px-5 py-3 bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 rounded-xl text-sm font-medium flex items-center gap-2"
//                   >
//                     <Send className="w-4 h-4" />
//                     Gönder
//                   </button>
//                 </form>
//               </div>
//             )}

//             {/* SEKME 3: YÖNETMELİK PDF & QDRANT RAG */}
//             {activeTab === 'rag' && (
//               <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
//                 <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5 space-y-4">
//                   <h3 className="font-semibold text-white">Yeni Yönetmelik PDF İndeksle</h3>
//                   <p className="text-xs text-slate-400">
//                     Planlı Alanlar İmar Yönetmeliği, Yangın Yönetmeliği veya şirket içi BIM standart PDF dosyalarını Qdrant vektör veritabanına gömün.
//                   </p>
//                   <div>
//                     <label className="block text-xs text-slate-400 mb-1">Yönetmelik Kategorisi</label>
//                     <input
//                       type="text"
//                       value={pdfCategory}
//                       onChange={(e) => setPdfCategory(e.target.value)}
//                       className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-sm text-white"
//                     />
//                   </div>
//                   <label className="w-full flex items-center justify-center gap-2 px-4 py-3 bg-slate-800 hover:bg-slate-700 border border-slate-700 rounded-xl cursor-pointer text-sm font-medium">
//                     {isPdfUploading ? <Loader2 className="w-4 h-4 animate-spin" /> : <Upload className="w-4 h-4" />}
//                     <span>{isPdfUploading ? 'PDF Parçalanıp Vektörleştiriliyor...' : 'PDF Dosyası Seç ve İndeksle'}</span>
//                     <input type="file" accept=".pdf" className="hidden" onChange={handlePdfUpload} disabled={isPdfUploading} />
//                   </label>
//                 </div>

//                 <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5 space-y-4">
//                   <h3 className="font-semibold text-white">Qdrant Anlamsal Kural Arama</h3>
//                   <form onSubmit={handleRagSearch} className="flex gap-2">
//                     <input
//                       type="text"
//                       value={ragQuery}
//                       onChange={(e) => setRagQuery(e.target.value)}
//                       placeholder="Örn: yangın kaçış kapısı genişliği..."
//                       className="flex-1 bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-sm text-white"
//                     />
//                     <button type="submit" className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 rounded-xl text-sm flex items-center gap-1">
//                       <Search className="w-4 h-4" />
//                       Ara
//                     </button>
//                   </form>

//                   <div className="space-y-2 max-h-64 overflow-y-auto">
//                     {ragResults.map((r, i) => (
//                       <div key={i} className="bg-slate-950 border border-slate-800 p-3 rounded-xl text-xs">
//                         <div className="flex justify-between font-semibold text-indigo-300 mb-1">
//                           <span>{r.ruleCode}</span>
//                           <span>Skor: {(r.score * 100).toFixed(1)}%</span>
//                         </div>
//                         <p className="text-slate-300">{r.content}</p>
//                       </div>
//                     ))}
//                   </div>
//                 </div>
//               </div>
//             )}
//           </div>
//         )}
//       </main>
//     </div>
//   );
// }

// export default App;