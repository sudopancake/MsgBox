/* global React, ReactDOM, MsgBoxApi, bootstrap */

const CURRENT_PERSON_KEY = "msgboxCurrentPersonId";

/** Stored in LiteDB / API as accentColor; drives html[data-msgbox-accent]. */
const ACCENT_SWATCHES = [
  { id: "blue", label: "Blue", color: "#0d6efd" },
  { id: "purple", label: "Purple", color: "#6f42c1" },
  { id: "seafoam", label: "Seafoam", color: "#4F7B73" },
  { id: "forestGreen", label: "Forest green", color: "#2d6a4f" },
  { id: "lightPink", label: "Light pink", color: "#f5c6e0" },
  { id: "orange", label: "Orange", color: "#ffb870" },
  { id: "red", label: "Red", color: "#c75c5c" },
  { id: "yellow", label: "Yellow", color: "#f5e08a" },
  { id: "gray", label: "Gray", color: "#7a8796" },
];

function normalizeAccentColor(value) {
  if (!value || typeof value !== "string") return "blue";
  if (value === "teal") return "seafoam";
  return ACCENT_SWATCHES.some((s) => s.id === value) ? value : "blue";
}

function applyTheme(theme) {
  const root = document.documentElement;
  root.setAttribute("data-bs-theme", theme.isDark ? "dark" : "light");
  root.setAttribute("data-msgbox-accent", normalizeAccentColor(theme.accentColor));
}

function useBootstrapModal(show, onClose) {
  const ref = React.useRef(null);
  const onCloseRef = React.useRef(onClose);
  React.useLayoutEffect(() => {
    onCloseRef.current = onClose;
  });

  React.useEffect(() => {
    const el = ref.current;
    if (!el) return;
    bootstrap.Modal.getOrCreateInstance(el, { backdrop: true, keyboard: true });
    const handleHidden = () => {
      if (onCloseRef.current) onCloseRef.current();
    };
    el.addEventListener("hidden.bs.modal", handleHidden);
    return () => el.removeEventListener("hidden.bs.modal", handleHidden);
  }, []);

  React.useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const inst = bootstrap.Modal.getOrCreateInstance(el);
    if (show) {
      inst.show();
    } else if (el.classList.contains("show")) {
      inst.hide();
    }
  }, [show]);

  return ref;
}

function formatShortTime(iso) {
  if (!iso) return "";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "";
  return d.toLocaleString(undefined, { month: "short", day: "numeric", hour: "numeric", minute: "2-digit" });
}

function ChatListItem({ chat, active, onSelect }) {
  return (
    <div
      className={"msgbox-chat-item" + (active ? " active" : "")}
      onClick={() => onSelect(chat.id)}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          onSelect(chat.id);
        }
      }}
    >
      <div className="fw-semibold">{chat.displayName}</div>
      <div className="msgbox-chat-preview">{chat.latestMessageText || "—"}</div>
      <div className="small text-muted">{formatShortTime(chat.latestMessageUtc)}</div>
    </div>
  );
}

function ChatList({ chats, selectedId, onSelect }) {
  return (
    <div className="msgbox-sidebar">
      <div className="msgbox-sidebar-header">Chats</div>
      <div className="flex-grow-1 overflow-auto">
        {(chats || []).map((c) => (
          <ChatListItem key={c.id} chat={c} active={c.id === selectedId} onSelect={onSelect} />
        ))}
        {(!chats || chats.length === 0) && <div className="p-3 text-muted small">No chats yet.</div>}
      </div>
    </div>
  );
}

function formatMessageTime(iso) {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "";
  return d.toLocaleString(undefined, {
    weekday: "long",
    month: "long",
    day: "numeric",
    year: "numeric",
    hour: "numeric",
    minute: "2-digit",
  });
}

function toDatetimeLocalValue(iso) {
  if (!iso) return "";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "";
  const pad = (n) => String(n).padStart(2, "0");
  return (
    d.getFullYear() +
    "-" +
    pad(d.getMonth() + 1) +
    "-" +
    pad(d.getDate()) +
    "T" +
    pad(d.getHours()) +
    ":" +
    pad(d.getMinutes())
  );
}

function messageReactionsToJson(message, people) {
  if (!message.reactions || !message.reactions.length) return "[]";
  const arr = message.reactions.map((r) => ({
    emoji: r.emoji,
    personIds: (r.peopleNames || [])
      .map((name) => {
        const p = (people || []).find((x) => x.displayName === name);
        return p ? p.id : null;
      })
      .filter(Boolean),
  }));
  return JSON.stringify(arr);
}

function initials(displayName) {
  if (!displayName) return "?";
  const parts = displayName.trim().split(/\s+/);
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

function ReactionPill({ reaction }) {
  const label = reaction.count >= 2 ? `${reaction.emoji} ${reaction.count}` : reaction.emoji;
  return (
    <span className="msgbox-reaction-pill" title={reaction.tooltipText || ""}>
      {label}
    </span>
  );
}

function MessageBubble({ message, isMe, onEdit }) {
  const a = message.author || {};
  const bubbleClass = "msgbox-bubble" + (isMe ? " outgoing" : " incoming");
  const alignMeta = isMe ? " text-end" : "";
  const editBtn =
    onEdit && message.id ? (
      <button
        type="button"
        className="btn btn-link btn-sm py-0 px-1 msgbox-msg-edit"
        onClick={() => onEdit(message.id)}
        title="Edit message"
      >
        Edit
      </button>
    ) : null;

  const bubbleBody = (
    <>
      <div className={bubbleClass}>
        <div>{message.text}</div>
        {(message.imageFiles || []).map((img) => (
          <div key={img.storageKey || img.path}>
            <a href={img.path} target="_blank" rel="noreferrer">
              <img className="msgbox-thumb" src={img.path} alt={img.fileName} />
            </a>
          </div>
        ))}
        {(message.attachments || []).map((f) => (
          <div key={f.storageKey || f.path}>
            <a href={f.path} download={f.fileName}>
              {f.fileName}
            </a>
          </div>
        ))}
      </div>
      {(message.reactions || []).length > 0 && (
        <div className={"msgbox-reactions" + (isMe ? " justify-content-end" : "")}>
          {(message.reactions || []).map((r, i) => (
            <ReactionPill key={i} reaction={r} />
          ))}
        </div>
      )}
    </>
  );

  if (isMe) {
    return (
      <div className="msgbox-block msgbox-block-outgoing">
        <div className={"msgbox-meta msgbox-outgoing-meta d-flex align-items-center justify-content-end gap-2 flex-wrap" + alignMeta}>
          {editBtn}
          <span>{formatMessageTime(message.sentUtc)}</span>
        </div>
        <div className="msgbox-bubble-wrap">{bubbleBody}</div>
      </div>
    );
  }

  return (
    <div className="msgbox-block msgbox-block-incoming">
      <div className="msgbox-incoming-grid">
        <div className={"msgbox-meta msgbox-incoming-meta d-flex align-items-center gap-2 flex-wrap" + alignMeta}>
          <span>
            <span className="fw-semibold me-1">{a.displayName}</span>
            {formatMessageTime(message.sentUtc)}
          </span>
          {editBtn}
        </div>
        <div className="msgbox-incoming-avatar">
          <div
            className="msgbox-avatar"
            style={{ backgroundColor: a.backColor || "#6c757d", color: a.foreColor || "#fff" }}
          >
            {a.avatarPath ? <img src={a.avatarPath} alt="" /> : initials(a.displayName)}
          </div>
        </div>
        <div className="msgbox-incoming-body">
          <div className="msgbox-bubble-wrap">{bubbleBody}</div>
        </div>
      </div>
    </div>
  );
}

function ChatHeader({ title }) {
  const line = title ? "Conversiont With - " + title : "Select a chat";
  return <div className="msgbox-main-header fw-semibold">{line}</div>;
}

function MessageThread({ chatId, currentPersonId, refreshTick, onEditMessage }) {
  const [messages, setMessages] = React.useState([]);
  const [loadingOlder, setLoadingOlder] = React.useState(false);
  const [hasMore, setHasMore] = React.useState(true);
  const [initializing, setInitializing] = React.useState(false);
  const containerRef = React.useRef(null);
  const skipScrollToBottomRef = React.useRef(false);
  const fetchingOlderRef = React.useRef(false);

  const loadInitial = React.useCallback(async () => {
    if (!chatId) {
      setMessages([]);
      setHasMore(true);
      return;
    }
    setInitializing(true);
    setMessages([]);
    skipScrollToBottomRef.current = false;
    fetchingOlderRef.current = false;
    try {
      const batch = await MsgBoxApi.getJson(`/api/messages/by-chat/${encodeURIComponent(chatId)}?take=25`);
      setMessages(batch || []);
      setHasMore((batch || []).length >= 25);
    } finally {
      setInitializing(false);
    }
  }, [chatId]);

  React.useEffect(() => {
    loadInitial();
  }, [loadInitial, refreshTick]);

  React.useLayoutEffect(() => {
    const el = containerRef.current;
    if (!el || skipScrollToBottomRef.current) return;
    const run = () => {
      el.scrollTop = el.scrollHeight;
    };
    requestAnimationFrame(() => requestAnimationFrame(run));
  }, [messages, chatId, initializing]);

  async function loadOlder() {
    if (!chatId || loadingOlder || !hasMore || messages.length === 0 || fetchingOlderRef.current) return;
    const first = messages[0];
    if (!first) return;

    fetchingOlderRef.current = true;
    setLoadingOlder(true);
    const el = containerRef.current;
    const prevHeight = el ? el.scrollHeight : 0;
    const prevTop = el ? el.scrollTop : 0;

    try {
      const batch = await MsgBoxApi.getJson(
        `/api/messages/by-chat/${encodeURIComponent(chatId)}?take=25&beforeMessageId=${encodeURIComponent(first.id)}`
      );
      if (!batch || batch.length === 0) {
        setHasMore(false);
        return;
      }
      skipScrollToBottomRef.current = true;
      setMessages((m) => [...batch, ...m]);
      setHasMore(batch.length >= 25);

      requestAnimationFrame(() => {
        requestAnimationFrame(() => {
          if (!el) return;
          const newH = el.scrollHeight;
          el.scrollTop = newH - prevHeight + prevTop;
          skipScrollToBottomRef.current = false;
        });
      });
    } finally {
      setLoadingOlder(false);
      fetchingOlderRef.current = false;
    }
  }

  function onScroll(e) {
    const el = e.target;
    if (el.scrollTop < 80) loadOlder();
  }

  if (!chatId) {
    return <div className="msgbox-placeholder">Select a chat to view messages.</div>;
  }

  return (
    <div className="msgbox-thread" ref={containerRef} onScroll={onScroll}>
      <div className="msgbox-load-older">
        {loadingOlder ? "Loading older…" : hasMore ? "Scroll up for older messages" : "Start of conversation"}
      </div>
      {initializing ? <div className="p-3 text-center text-muted">Loading…</div> : null}
      {(messages || []).map((m) => (
        <MessageBubble
          key={m.id}
          message={m}
          isMe={!!currentPersonId && m.author && m.author.id === currentPersonId}
          onEdit={onEditMessage}
        />
      ))}
    </div>
  );
}

function GearMenu({ onAddPerson, onAddChat, onAddMessage, onAddMessagesMultiple, onTheme }) {
  return (
    <div className="dropdown msgbox-gear-wrap">
      <button
        className="btn btn-sm dropdown-toggle msgbox-gear-btn"
        type="button"
        data-bs-toggle="dropdown"
        data-bs-display="static"
        aria-expanded="false"
        aria-label="Menu"
        title="Menu"
      >
        ⚙
      </button>
      <ul className="dropdown-menu dropdown-menu-end shadow">
        <li>
          <button className="dropdown-item" type="button" onClick={onAddPerson}>
            Add Person
          </button>
        </li>
        <li>
          <button className="dropdown-item" type="button" onClick={onAddChat}>
            Add Chat
          </button>
        </li>
        <li>
          <button className="dropdown-item" type="button" onClick={onAddMessage}>
            Add message
          </button>
        </li>
        <li>
          <button className="dropdown-item" type="button" onClick={onAddMessagesMultiple}>
            Add messages (multiple)
          </button>
        </li>
        <li>
          <hr className="dropdown-divider" />
        </li>
        <li>
          <button className="dropdown-item" type="button" onClick={onTheme}>
            Theme
          </button>
        </li>
      </ul>
    </div>
  );
}

function AddPersonModal({ show, onClose, onSaved }) {
  const ref = useBootstrapModal(show, onClose);
  const [error, setError] = React.useState("");

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");
    const fd = new FormData(e.target);
    try {
      await MsgBoxApi.postForm("/api/people", fd);
      e.target.reset();
      await onSaved();
      onClose();
    } catch (err) {
      setError(err.message || String(err));
    }
  }

  return (
    <div className="modal fade" ref={ref} tabIndex="-1" aria-hidden="true">
      <div className="modal-dialog">
        <form onSubmit={handleSubmit}>
          <div className="modal-content">
            <div className="modal-header">
              <h5 className="modal-title">Add Person</h5>
              <button type="button" className="btn-close" data-bs-dismiss="modal" aria-label="Close" />
            </div>
            <div className="modal-body">
              {error ? <div className="alert alert-danger py-1">{error}</div> : null}
              <div className="mb-2">
                <label className="form-label">First name</label>
                <input name="firstName" className="form-control" required />
              </div>
              <div className="mb-2">
                <label className="form-label">Last name</label>
                <input name="lastName" className="form-control" required />
              </div>
              <div className="mb-2">
                <label className="form-label">Display name (optional)</label>
                <input name="displayName" className="form-control" />
              </div>
              <div className="row">
                <div className="col mb-2">
                  <label className="form-label">Text color</label>
                  <input name="foreColor" type="color" className="form-control form-control-color" defaultValue="#ffffff" />
                </div>
                <div className="col mb-2">
                  <label className="form-label">Bubble / avatar color</label>
                  <input name="backColor" type="color" className="form-control form-control-color" defaultValue="#0d6efd" />
                </div>
              </div>
              <div className="mb-2">
                <label className="form-label">Avatar (optional)</label>
                <input name="avatar" type="file" className="form-control" accept="image/*" />
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" data-bs-dismiss="modal">
                Cancel
              </button>
              <button type="submit" className="btn btn-primary">
                Save
              </button>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}

function AddChatModal({ show, onClose, onSaved, people }) {
  const ref = useBootstrapModal(show, onClose);
  const [chatName, setChatName] = React.useState("");
  const [chatType, setChatType] = React.useState("person");
  const [selected, setSelected] = React.useState({});
  const [error, setError] = React.useState("");

  function togglePerson(id) {
    setSelected((s) => ({ ...s, [id]: !s[id] }));
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");
    const personIds = Object.keys(selected).filter((id) => selected[id]);
    if (personIds.length === 0) {
      setError("Select at least one person.");
      return;
    }
    try {
      await MsgBoxApi.postJson("/api/chats", {
        chatName: chatName || null,
        chatType,
        personIds,
      });
      setChatName("");
      setSelected({});
      await onSaved();
      onClose();
    } catch (err) {
      setError(err.message || String(err));
    }
  }

  return (
    <div className="modal fade" ref={ref} tabIndex="-1" aria-hidden="true">
      <div className="modal-dialog modal-lg">
        <form onSubmit={handleSubmit}>
          <div className="modal-content">
            <div className="modal-header">
              <h5 className="modal-title">Add Chat</h5>
              <button type="button" className="btn-close" data-bs-dismiss="modal" aria-label="Close" />
            </div>
            <div className="modal-body">
              {error ? <div className="alert alert-danger py-1">{error}</div> : null}
              <div className="mb-2">
                <label className="form-label">Chat name (optional)</label>
                <input className="form-control" value={chatName} onChange={(e) => setChatName(e.target.value)} />
              </div>
              <div className="mb-3">
                <label className="form-label">Chat type</label>
                <select className="form-select" value={chatType} onChange={(e) => setChatType(e.target.value)}>
                  <option value="person">Person</option>
                  <option value="group">Group</option>
                </select>
              </div>
              <label className="form-label">People</label>
              <div className="list-group">
                {(people || []).map((p) => (
                  <label key={p.id} className="list-group-item d-flex gap-2">
                    <input
                      className="form-check-input flex-shrink-0"
                      type="checkbox"
                      checked={!!selected[p.id]}
                      onChange={() => togglePerson(p.id)}
                    />
                    <span>
                      {p.displayName} <small className="text-muted">({p.id})</small>
                    </span>
                  </label>
                ))}
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" data-bs-dismiss="modal">
                Cancel
              </button>
              <button type="submit" className="btn btn-primary">
                Save
              </button>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}

function AddMessageModal({ show, onClose, onSaved, chats, people, defaultChatId }) {
  const ref = useBootstrapModal(show, onClose);
  const [error, setError] = React.useState("");
  const [chatId, setChatId] = React.useState("");
  const [authorId, setAuthorId] = React.useState("");
  const [reactionEmoji, setReactionEmoji] = React.useState("");
  const [reactionPeople, setReactionPeople] = React.useState({});

  React.useEffect(() => {
    if (show && defaultChatId) setChatId(defaultChatId);
  }, [show, defaultChatId]);

  function toggleReactionPerson(id) {
    setReactionPeople((s) => ({ ...s, [id]: !s[id] }));
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");
    const form = e.target;
    const fd = new FormData();
    fd.append("chatId", chatId);
    fd.append("authorPersonId", authorId);
    const local = form.sentLocal.value;
    if (!local) {
      setError("Sent date/time required.");
      return;
    }
    const sent = new Date(local);
    fd.append("sentUtc", sent.toISOString());
    fd.append("text", form.text.value || "");

    const imgs = form.images.files;
    for (let i = 0; i < imgs.length; i++) fd.append("images", imgs[i]);
    const atts = form.attachments.files;
    for (let i = 0; i < atts.length; i++) fd.append("attachments", atts[i]);

    const rIds = Object.keys(reactionPeople).filter((id) => reactionPeople[id]);
    if (reactionEmoji && rIds.length) {
      fd.append("reactionsJson", JSON.stringify([{ emoji: reactionEmoji, personIds: rIds }]));
    }

    try {
      await MsgBoxApi.postForm("/api/messages", fd);
      form.reset();
      setReactionEmoji("");
      setReactionPeople({});
      await onSaved();
      onClose();
    } catch (err) {
      setError(err.message || String(err));
    }
  }

  return (
    <div className="modal fade" ref={ref} tabIndex="-1" aria-hidden="true">
      <div className="modal-dialog modal-lg">
        <form onSubmit={handleSubmit}>
          <div className="modal-content">
            <div className="modal-header">
              <h5 className="modal-title">Add Message</h5>
              <button type="button" className="btn-close" data-bs-dismiss="modal" aria-label="Close" />
            </div>
            <div className="modal-body">
              {error ? <div className="alert alert-danger py-1">{error}</div> : null}
              <div className="mb-2">
                <label className="form-label">Chat</label>
                <select className="form-select" value={chatId} onChange={(e) => setChatId(e.target.value)} required>
                  <option value="">Select…</option>
                  {(chats || []).map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.displayName}
                    </option>
                  ))}
                </select>
              </div>
              <div className="mb-2">
                <label className="form-label">Author</label>
                <select className="form-select" value={authorId} onChange={(e) => setAuthorId(e.target.value)} required>
                  <option value="">Select…</option>
                  {(people || []).map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.displayName}
                    </option>
                  ))}
                </select>
              </div>
              <div className="mb-2">
                <label className="form-label">Sent (local)</label>
                <input name="sentLocal" type="datetime-local" className="form-control" required />
              </div>
              <div className="mb-2">
                <label className="form-label">Text</label>
                <textarea name="text" className="form-control" rows={4} />
              </div>
              <div className="mb-2">
                <label className="form-label">Images</label>
                <input name="images" type="file" className="form-control" multiple accept="image/*" />
              </div>
              <div className="mb-2">
                <label className="form-label">Attachments</label>
                <input name="attachments" type="file" className="form-control" multiple />
              </div>
              <div className="border rounded p-2 mb-2">
                <div className="small text-muted mb-1">Optional reaction</div>
                <div className="mb-2">
                  <label className="form-label">Emoji</label>
                  <input
                    className="form-control"
                    value={reactionEmoji}
                    onChange={(e) => setReactionEmoji(e.target.value)}
                    placeholder="👍"
                  />
                </div>
                <label className="form-label">Reacting people</label>
                <div className="d-flex flex-wrap gap-2">
                  {(people || []).map((p) => (
                    <label key={p.id} className="form-check form-check-inline">
                      <input
                        className="form-check-input"
                        type="checkbox"
                        checked={!!reactionPeople[p.id]}
                        onChange={() => toggleReactionPerson(p.id)}
                      />
                      <span className="form-check-label">{p.displayName}</span>
                    </label>
                  ))}
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" data-bs-dismiss="modal">
                Cancel
              </button>
              <button type="submit" className="btn btn-primary">
                Save
              </button>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}

function BulkImportModal({ show, onClose, onImported, chats, defaultChatId }) {
  const ref = useBootstrapModal(show, onClose);
  const [chatId, setChatId] = React.useState("");
  const [jsonText, setJsonText] = React.useState("");
  const [preview, setPreview] = React.useState(null);
  const [commitResult, setCommitResult] = React.useState(null);
  const [busy, setBusy] = React.useState(false);
  const [error, setError] = React.useState("");

  React.useEffect(() => {
    if (show && defaultChatId) setChatId(defaultChatId);
  }, [show, defaultChatId]);

  React.useEffect(() => {
    if (!show) {
      setPreview(null);
      setCommitResult(null);
      setError("");
    }
  }, [show]);

  async function runPreview() {
    setError("");
    setCommitResult(null);
    setBusy(true);
    try {
      const res = await MsgBoxApi.postJson("/api/messages/import/preview", { chatId, json: jsonText });
      setPreview(res);
      if (!res.parseOk) setError(res.parseError || "Could not parse JSON.");
    } catch (err) {
      setPreview(null);
      setError(err.message || String(err));
    } finally {
      setBusy(false);
    }
  }

  async function runImport() {
    setError("");
    setCommitResult(null);
    setBusy(true);
    try {
      const res = await MsgBoxApi.postJson("/api/messages/import/commit", { chatId, json: jsonText });
      setCommitResult(res);
      const failed = res.failedCount || 0;
      const ok = res.importedCount || 0;
      if (ok > 0) await onImported();
      if (failed > 0 && ok === 0) {
        const detail = (res.failures || [])
          .map((f) => "Row " + f.index + ": " + (f.reasons || []).join("; "))
          .join(" | ");
        setError("Import completed with failures. " + detail);
      }
    } catch (err) {
      setError(err.message || String(err));
    } finally {
      setBusy(false);
    }
  }

  const canImport =
    preview &&
    preview.parseOk &&
    preview.validCount > 0 &&
    chatId &&
    !busy;

  return (
    <div className="modal fade" ref={ref} tabIndex="-1" aria-hidden="true">
      <div className="modal-dialog modal-xl modal-dialog-scrollable">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">Bulk import (JSON)</h5>
            <button type="button" className="btn-close" data-bs-dismiss="modal" aria-label="Close" />
          </div>
          <div className="modal-body">
            {error ? <div className="alert alert-danger py-2 small">{error}</div> : null}
            <div className="mb-2">
              <label className="form-label">Chat</label>
              <select className="form-select" value={chatId} onChange={(e) => setChatId(e.target.value)} required>
                <option value="">Select…</option>
                {(chats || []).map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.displayName}
                  </option>
                ))}
              </select>
            </div>
            <div className="mb-2">
              <label className="form-label">Message JSON (array)</label>
              <textarea
                className="form-control font-monospace msgbox-bulk-json"
                rows={12}
                value={jsonText}
                onChange={(e) => setJsonText(e.target.value)}
                placeholder={"[\n  { \"by\": \"Jane Doe\", \"date\": \"2026-03-24T15:55:00\", \"message\": \"…\", \"emojis\": [] }\n]"}
              />
            </div>
            <div className="d-flex flex-wrap gap-2 mb-3">
              <button type="button" className="btn btn-outline-secondary" disabled={busy || !chatId} onClick={runPreview}>
                {busy ? "Working…" : "Preview & validate"}
              </button>
              <button type="button" className="btn btn-primary" disabled={!canImport} onClick={runImport}>
                Import valid rows
              </button>
            </div>

            {commitResult ? (
              <div className="alert alert-info py-2 small mb-3">
                Imported <strong>{commitResult.importedCount}</strong>, failed <strong>{commitResult.failedCount}</strong>.
                {commitResult.importedIds && commitResult.importedIds.length ? (
                  <span className="d-block text-muted mt-1">IDs: {commitResult.importedIds.slice(0, 5).join(", ")}
                    {commitResult.importedIds.length > 5 ? "…" : ""}
                  </span>
                ) : null}
              </div>
            ) : null}

            {preview && preview.parseOk ? (
              <div className="mb-2 small text-muted">
                Parsed <strong>{preview.totalRows}</strong> rows — <strong className="text-success">{preview.validCount}</strong> valid,{" "}
                <strong className="text-danger">{preview.invalidCount}</strong> invalid.
              </div>
            ) : null}

            {preview && preview.rows && preview.rows.length ? (
              <div className="table-responsive msgbox-bulk-preview border rounded">
                <table className="table table-sm table-striped mb-0 align-middle">
                  <thead className="table-secondary">
                    <tr>
                      <th>#</th>
                      <th>OK</th>
                      <th>By</th>
                      <th>Date</th>
                      <th>Message</th>
                      <th>Reactions</th>
                      <th>Issues</th>
                    </tr>
                  </thead>
                  <tbody>
                    {preview.rows.map((row) => (
                      <tr key={row.index} className={row.isValid ? "" : "table-warning"}>
                        <td>{row.index}</td>
                        <td>{row.isValid ? "✓" : "✗"}</td>
                        <td>{(row.preview && row.preview.by) || "—"}</td>
                        <td className="text-nowrap">{row.preview && row.preview.date}</td>
                        <td className="small">{(row.preview && row.preview.messageSnippet) || "—"}</td>
                        <td className="small">{(row.preview && row.preview.reactionSummary) || "—"}</td>
                        <td className="small text-danger">
                          {(row.errors || []).length ? (row.errors || []).join("; ") : "—"}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : null}
          </div>
          <div className="modal-footer">
            <button type="button" className="btn btn-secondary" data-bs-dismiss="modal">
              Close
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

function EditMessageModal({ show, messageId, onClose, onSaved, chats, people }) {
  const ref = useBootstrapModal(show, onClose);
  const peopleRef = React.useRef(people);
  React.useLayoutEffect(() => {
    peopleRef.current = people;
  });
  const [error, setError] = React.useState("");
  const [loading, setLoading] = React.useState(false);
  const [chatId, setChatId] = React.useState("");
  const [authorId, setAuthorId] = React.useState("");
  const [sentLocal, setSentLocal] = React.useState("");
  const [text, setText] = React.useState("");
  const [reactionsJson, setReactionsJson] = React.useState("[]");
  const [existingImages, setExistingImages] = React.useState([]);
  const [existingAttachments, setExistingAttachments] = React.useState([]);
  const [removeImagePaths, setRemoveImagePaths] = React.useState({});
  const [removeAttachmentPaths, setRemoveAttachmentPaths] = React.useState({});

  React.useEffect(() => {
    if (!show || !messageId) {
      setLoading(false);
      return;
    }
    let cancelled = false;
    (async () => {
      setError("");
      setLoading(true);
      try {
        const m = await MsgBoxApi.getJson("/api/messages/" + encodeURIComponent(messageId));
        if (cancelled) return;
        setChatId(m.chatId || "");
        setAuthorId((m.author && m.author.id) || "");
        setSentLocal(toDatetimeLocalValue(m.sentUtc));
        setText(m.text || "");
        setReactionsJson(messageReactionsToJson(m, peopleRef.current));
        setExistingImages(m.imageFiles || []);
        setExistingAttachments(m.attachments || []);
        setRemoveImagePaths({});
        setRemoveAttachmentPaths({});
      } catch (err) {
        if (!cancelled) setError(err.message || String(err));
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [show, messageId]);

  function togglePathRemove(setMap, path) {
    setMap((s) => ({ ...s, [path]: !s[path] }));
  }

  function fileKey(file) {
    return file.storageKey || file.path;
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");
    if (!messageId || !chatId || !authorId) {
      setError("Missing message, chat, or author.");
      return;
    }
    const fd = new FormData();
    fd.append("chatId", chatId);
    fd.append("authorPersonId", authorId);
    const form = e.target;
    const imgs = form.images.files;
    for (let i = 0; i < imgs.length; i++) fd.append("images", imgs[i]);
    const atts = form.attachments.files;
    for (let i = 0; i < atts.length; i++) fd.append("attachments", atts[i]);
    const local = sentLocal;
    if (!local) {
      setError("Sent date/time required.");
      return;
    }
    fd.append("sentUtc", new Date(local).toISOString());
    fd.append("text", text || "");

    let reactions;
    try {
      reactions = JSON.parse(reactionsJson || "[]");
      if (!Array.isArray(reactions)) throw new Error("Reactions must be a JSON array.");
    } catch (err) {
      setError("Invalid reactions JSON: " + (err.message || err));
      return;
    }
    fd.append("reactionsJson", JSON.stringify(reactions));

    const rip = Object.keys(removeImagePaths).filter((p) => removeImagePaths[p]);
    const rap = Object.keys(removeAttachmentPaths).filter((p) => removeAttachmentPaths[p]);
    fd.append("removeImagePathsJson", JSON.stringify(rip));
    fd.append("removeAttachmentPathsJson", JSON.stringify(rap));

    try {
      await MsgBoxApi.putForm("/api/messages/" + encodeURIComponent(messageId), fd);
      await onSaved();
      onClose();
    } catch (err) {
      setError(err.message || String(err));
    }
  }

  return (
    <div className="modal fade" ref={ref} tabIndex="-1" aria-hidden="true">
      <div className="modal-dialog modal-lg">
        <form onSubmit={handleSubmit}>
          <div className="modal-content">
            <div className="modal-header">
              <h5 className="modal-title">Edit message</h5>
              <button type="button" className="btn-close" data-bs-dismiss="modal" aria-label="Close" />
            </div>
            <div className="modal-body">
              {error ? <div className="alert alert-danger py-1">{error}</div> : null}
              {loading ? <div className="text-muted small">Loading message…</div> : null}
              {!loading && messageId ? (
                <>
                  <div className="mb-2">
                    <label className="form-label">Chat</label>
                    <input
                      type="text"
                      className="form-control"
                      readOnly
                      value={(chats || []).find((c) => c.id === chatId)?.displayName || chatId || ""}
                    />
                  </div>
                  <div className="mb-2">
                    <label className="form-label">Author</label>
                    <select className="form-select" value={authorId} onChange={(e) => setAuthorId(e.target.value)} required>
                      <option value="">Select…</option>
                      {(people || []).map((p) => (
                        <option key={p.id} value={p.id}>
                          {p.displayName}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div className="mb-2">
                    <label className="form-label">Sent (local)</label>
                    <input
                      type="datetime-local"
                      className="form-control"
                      value={sentLocal}
                      onChange={(e) => setSentLocal(e.target.value)}
                      required
                    />
                  </div>
                  <div className="mb-2">
                    <label className="form-label">Text</label>
                    <textarea className="form-control" rows={4} value={text} onChange={(e) => setText(e.target.value)} />
                  </div>

                  {(existingImages || []).length ? (
                    <div className="mb-2">
                      <label className="form-label">Existing images — remove?</label>
                      <ul className="list-unstyled small mb-0">
                        {existingImages.map((img) => (
                          <li key={fileKey(img)}>
                            <label className="form-check">
                              <input
                                className="form-check-input"
                                type="checkbox"
                                checked={!!removeImagePaths[fileKey(img)]}
                                onChange={() => togglePathRemove(setRemoveImagePaths, fileKey(img))}
                              />
                              <span className="form-check-label">{img.fileName}</span>
                            </label>
                          </li>
                        ))}
                      </ul>
                    </div>
                  ) : null}
                  {(existingAttachments || []).length ? (
                    <div className="mb-2">
                      <label className="form-label">Existing attachments — remove?</label>
                      <ul className="list-unstyled small mb-0">
                        {existingAttachments.map((f) => (
                          <li key={fileKey(f)}>
                            <label className="form-check">
                              <input
                                className="form-check-input"
                                type="checkbox"
                                checked={!!removeAttachmentPaths[fileKey(f)]}
                                onChange={() => togglePathRemove(setRemoveAttachmentPaths, fileKey(f))}
                              />
                              <span className="form-check-label">{f.fileName}</span>
                            </label>
                          </li>
                        ))}
                      </ul>
                    </div>
                  ) : null}

                  <div className="mb-2">
                    <label className="form-label">Add images</label>
                    <input name="images" type="file" className="form-control" multiple accept="image/*" />
                  </div>
                  <div className="mb-2">
                    <label className="form-label">Add attachments</label>
                    <input name="attachments" type="file" className="form-control" multiple />
                  </div>
                  <div className="mb-2">
                    <label className="form-label">Reactions (JSON: array of {"{"} emoji, personIds {"}"})</label>
                    <textarea
                      className="form-control font-monospace"
                      rows={4}
                      value={reactionsJson}
                      onChange={(e) => setReactionsJson(e.target.value)}
                    />
                  </div>
                </>
              ) : null}
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" data-bs-dismiss="modal">
                Cancel
              </button>
              <button type="submit" className="btn btn-primary" disabled={loading || !messageId}>
                Save
              </button>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}

function ThemeModal({ show, onClose, theme, onSave, people, currentPersonId, onChangePerson }) {
  const ref = useBootstrapModal(show, onClose);
  const [isDark, setIsDark] = React.useState(true);
  const [accent, setAccent] = React.useState("blue");
  const [me, setMe] = React.useState("");
  const [error, setError] = React.useState("");

  React.useEffect(() => {
    if (theme) {
      setIsDark(!!theme.isDark);
      setAccent(normalizeAccentColor(theme.accentColor));
    }
  }, [theme, show]);

  React.useEffect(() => {
    setMe(currentPersonId || "");
  }, [currentPersonId, show]);

  React.useEffect(() => {
    if (!show) setError("");
  }, [show]);

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");
    try {
      await onSave({ isDark, accentColor: normalizeAccentColor(accent) });
      if (onChangePerson) await onChangePerson(me || null);
      onClose();
    } catch (err) {
      setError(err.message || String(err));
    }
  }

  return (
    <div className="modal fade" ref={ref} tabIndex="-1" aria-hidden="true">
      <div className="modal-dialog">
        <form onSubmit={handleSubmit}>
          <div className="modal-content">
            <div className="modal-header">
              <h5 className="modal-title">Theme</h5>
              <button type="button" className="btn-close" data-bs-dismiss="modal" aria-label="Close" />
            </div>
            <div className="modal-body">
              {error ? <div className="alert alert-danger py-1">{error}</div> : null}
              <div className="form-check form-switch mb-3">
                <input
                  className="form-check-input"
                  type="checkbox"
                  checked={isDark}
                  onChange={(e) => setIsDark(e.target.checked)}
                  id="darkSwitch"
                />
                <label className="form-check-label" htmlFor="darkSwitch">
                  Dark mode
                </label>
              </div>
              <div className="mb-3">
                <label className="form-label d-block">Accent</label>
                <div className="msgbox-accent-swatches" role="radiogroup" aria-label="Accent color">
                  {ACCENT_SWATCHES.map((s) => (
                    <button
                      key={s.id}
                      type="button"
                      className={
                        "msgbox-accent-swatch" + (accent === s.id ? " msgbox-accent-swatch--selected" : "")
                      }
                      style={{ backgroundColor: s.color }}
                      title={s.label}
                      aria-label={s.label}
                      aria-checked={accent === s.id}
                      role="radio"
                      onClick={() => setAccent(s.id)}
                    />
                  ))}
                </div>
              </div>
              <div className="mb-1">
                <label className="form-label">Your profile (outgoing bubbles)</label>
                <select className="form-select" value={me} onChange={(e) => setMe(e.target.value)}>
                  <option value="">Not set</option>
                  {(people || []).map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.displayName}
                    </option>
                  ))}
                </select>
                <div className="form-text">Choose who you are so your messages align right with the accent bubble.</div>
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" data-bs-dismiss="modal">
                Cancel
              </button>
              <button type="submit" className="btn btn-primary">
                Save
              </button>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}

function App() {
  const [chats, setChats] = React.useState([]);
  const [people, setPeople] = React.useState([]);
  const [selectedChatId, setSelectedChatId] = React.useState(null);
  const [chatTitle, setChatTitle] = React.useState("");
  const [theme, setTheme] = React.useState({ isDark: true, accentColor: "blue" });
  const [currentPersonId, setCurrentPersonId] = React.useState(() => localStorage.getItem(CURRENT_PERSON_KEY) || "");
  const [refreshTick, setRefreshTick] = React.useState(0);

  const [showPerson, setShowPerson] = React.useState(false);
  const [showChat, setShowChat] = React.useState(false);
  const [showMessage, setShowMessage] = React.useState(false);
  const [showBulkImport, setShowBulkImport] = React.useState(false);
  const [showTheme, setShowTheme] = React.useState(false);
  const [editMessageId, setEditMessageId] = React.useState(null);

  async function refreshChats(forPersonIdOverride) {
    const me =
      forPersonIdOverride !== undefined && forPersonIdOverride !== null
        ? forPersonIdOverride
        : currentPersonId || localStorage.getItem(CURRENT_PERSON_KEY) || "";
    const q = me ? "?forPersonId=" + encodeURIComponent(me) : "";
    const list = await MsgBoxApi.getJson("/api/chats" + q);
    setChats(list || []);
    return list || [];
  }

  async function refreshPeople() {
    const list = await MsgBoxApi.getJson("/api/people");
    setPeople(list || []);
    return list || [];
  }

  React.useEffect(() => {
    (async () => {
      try {
        const s = await MsgBoxApi.getJson("/api/settings");
        const t = { isDark: !!s.isDark, accentColor: normalizeAccentColor(s.accentColor) };
        setTheme(t);
        applyTheme(t);

        const plist = await refreshPeople();
        let meId = localStorage.getItem(CURRENT_PERSON_KEY);
        if (!meId && plist && plist.length > 0) {
          meId = plist[0].id;
          localStorage.setItem(CURRENT_PERSON_KEY, meId);
        }
        setCurrentPersonId(meId || "");

        const list = await refreshChats(meId || "");
        setSelectedChatId((prev) => (!prev && list.length > 0 ? list[0].id : prev));
      } catch (e) {
        console.error(e);
      }
    })();
  }, []);

  React.useEffect(() => {
    (async () => {
      if (!selectedChatId) {
        setChatTitle("");
        return;
      }
      const row = chats.find((c) => c.id === selectedChatId);
      if (row) {
        setChatTitle(row.displayName);
        return;
      }
      try {
        const detail = await MsgBoxApi.getJson("/api/chats/" + encodeURIComponent(selectedChatId));
        const name =
          detail.chatName && detail.chatName.trim()
            ? detail.chatName
            : (detail.people || []).map((p) => p.displayName).join(", ");
        setChatTitle(name || "Chat");
      } catch {
        setChatTitle("Chat");
      }
    })();
  }, [selectedChatId, chats]);

  async function handleSaveTheme(t) {
    const body = { isDark: t.isDark, accentColor: t.accentColor };
    const saved = await MsgBoxApi.putJson("/api/settings/theme", body);
    const next = { isDark: !!saved.isDark, accentColor: normalizeAccentColor(saved.accentColor) };
    setTheme(next);
    applyTheme(next);
  }

  async function handleChangePerson(id) {
    if (id) {
      localStorage.setItem(CURRENT_PERSON_KEY, id);
      setCurrentPersonId(id);
      await refreshChats(id);
    } else {
      localStorage.removeItem(CURRENT_PERSON_KEY);
      setCurrentPersonId("");
      await refreshChats("");
    }
  }

  async function afterMutation() {
    const list = await refreshChats();
    setRefreshTick((x) => x + 1);
    setSelectedChatId((prev) => {
      if (!list.length) return null;
      if (prev && list.some((c) => c.id === prev)) return prev;
      return list[0].id;
    });
  }

  return (
    <div className="msgbox-shell">
      <ChatList chats={chats} selectedId={selectedChatId} onSelect={setSelectedChatId} />
      <div className="msgbox-main">
        <GearMenu
          onAddPerson={() => setShowPerson(true)}
          onAddChat={() => setShowChat(true)}
          onAddMessage={() => setShowMessage(true)}
          onAddMessagesMultiple={() => setShowBulkImport(true)}
          onTheme={() => setShowTheme(true)}
        />
        <ChatHeader title={chatTitle} />
        <MessageThread
          chatId={selectedChatId}
          currentPersonId={currentPersonId}
          refreshTick={refreshTick}
          onEditMessage={(id) => setEditMessageId(id)}
        />
      </div>

      <AddPersonModal
        show={showPerson}
        onClose={() => setShowPerson(false)}
        onSaved={refreshPeople}
      />
      <AddChatModal show={showChat} onClose={() => setShowChat(false)} onSaved={afterMutation} people={people} />
      <AddMessageModal
        show={showMessage}
        onClose={() => setShowMessage(false)}
        onSaved={afterMutation}
        chats={chats}
        people={people}
        defaultChatId={selectedChatId}
      />
      <BulkImportModal
        show={showBulkImport}
        onClose={() => setShowBulkImport(false)}
        onImported={afterMutation}
        chats={chats}
        defaultChatId={selectedChatId}
      />
      <EditMessageModal
        show={!!editMessageId}
        messageId={editMessageId}
        onClose={() => setEditMessageId(null)}
        onSaved={afterMutation}
        chats={chats}
        people={people}
      />
      <ThemeModal
        show={showTheme}
        onClose={() => setShowTheme(false)}
        theme={theme}
        onSave={handleSaveTheme}
        people={people}
        currentPersonId={currentPersonId}
        onChangePerson={handleChangePerson}
      />
    </div>
  );
}

const mountEl = document.getElementById("app");
if (!mountEl) {
  console.error("MsgBox: #app not found");
} else if (typeof ReactDOM.createRoot === "function") {
  ReactDOM.createRoot(mountEl).render(<App />);
} else {
  ReactDOM.render(<App />, mountEl);
}
