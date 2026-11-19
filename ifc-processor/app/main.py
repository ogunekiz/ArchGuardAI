import os
import shutil
import tempfile
from fastapi import FastAPI, UploadFile, File, HTTPException
from app.ifc_parser import parse_ifc_file

app = FastAPI(
    title="ArchGuard IFC Processor Service",
    description="BIM (.ifc) dosyalarını ayrıştırıp yapısal metraj ve eleman verisi çıkaran servis",
    version="1.0.0"
)


@app.get("/health")
def health_check():
    return {"status": "healthy", "service": "ifc-processor"}


@app.post("/parse-ifc")
async def parse_ifc_endpoint(file: UploadFile = File(...)):
    if not file.filename or not file.filename.lower().endswith(".ifc"):
        raise HTTPException(status_code=400, detail="Lütfen geçerli bir .ifc dosyası yükleyin.")

    tmp_path = None
    try:
        with tempfile.NamedTemporaryFile(delete=False, suffix=".ifc") as tmp:
            shutil.copyfileobj(file.file, tmp)
            tmp_path = tmp.name

        parsed_result = parse_ifc_file(tmp_path)
        parsed_result["source_file"] = file.filename
        return parsed_result

    except Exception as ex:
        raise HTTPException(status_code=500, detail=f"IFC dosyası işlenirken hata oluştu: {str(ex)}")
    finally:
        if tmp_path and os.path.exists(tmp_path):
            os.remove(tmp_path)