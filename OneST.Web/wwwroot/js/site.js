(() => {
  const root = document.getElementById("uen-history");
  if (!root) return;
  const key = "onest.uen-history.v1";
  let history = [];

  try {
    const saved = JSON.parse(sessionStorage.getItem(key) ?? "[]");
    if (Array.isArray(saved)) history = saved.filter(item =>
      typeof item?.uen === "string" && typeof item.valid === "boolean"
    ).slice(0, 10);
  } catch {  }

  if (["true", "false"].includes(root.dataset.valid)) {
    history.unshift({ uen: root.dataset.uen.trim(), valid: root.dataset.valid === "true" });
    history = history.slice(0, 10);
    try { sessionStorage.setItem(key, JSON.stringify(history)); }
    catch {  }
  }

  const list = document.getElementById("history-list");
  list.replaceChildren();
  for (const check of history) {
    const row = document.createElement("li");
    row.className = "history-entry";
    const uen = document.createElement("span");
    uen.className = "text-break";
    uen.textContent = check.uen;
    const result = document.createElement("span");
    result.className = check.valid
      ? "history-result history-result-valid"
      : "history-result history-result-invalid";
    result.textContent = check.valid ? "Valid format" : "Invalid format";
    row.append(uen, result);
    list.append(row);
  }
  document.getElementById("history-empty").hidden = history.length > 0;
})();
