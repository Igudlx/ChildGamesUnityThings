const database = {
  Scripts: [],
  Packages: [],
  Models: [],
  Materials: []
};

// STATE
let currentTab = "Home";
let currentList = [];
let historyStack = [];

// FLATTEN ALL ITEMS FOR HOME
function getAllItems() {
  return Object.keys(database).flatMap(category =>
    database[category].map(item => ({ ...item, category }))
  );
}

// SLIDESHOW
let slideIndex = 0;

function renderSlide() {
  const items = getAllItems();
  const container = document.getElementById("slideContent");

  if (items.length === 0) {
    container.innerHTML = "Nothing Here Yet";
    return;
  }

  const item = items[slideIndex];

  container.innerHTML = `
    <strong>${item.name}</strong>
    <p>${item.description}</p>
    <button onclick="openDetail('${item.category}', ${slideIndex})">View</button>
  `;
}

function nextSlide() {
  const items = getAllItems();
  if (items.length === 0) return;

  slideIndex = (slideIndex + 1) % items.length;
  renderSlide();
}

function prevSlide() {
  const items = getAllItems();
  if (items.length === 0) return;

  slideIndex = (slideIndex - 1 + items.length) % items.length;
  renderSlide();
}

// AUTO ROTATE
setInterval(nextSlide, 15000);

// TABS
function openTab(tab) {
  currentTab = tab;
  historyStack.push("home");

  const content = document.getElementById("content");
  const detail = document.getElementById("detailView");

  detail.classList.add("hidden");
  content.innerHTML = "";

  const items = database[tab];

  if (!items || items.length === 0) {
    content.innerHTML = "<p>Nothing Is Here Yet</p>";
    return;
  }

  items.forEach((item, index) => {
    const div = document.createElement("div");
    div.className = "item";
    div.innerHTML = `<strong>${item.name}</strong><br>${item.description}`;
    div.onclick = () => openDetail(tab, index);
    content.appendChild(div);
  });
}

// DETAIL VIEW
function openDetail(category, index) {
  const item = database[category][index];

  document.getElementById("content").innerHTML = "";
  const detail = document.getElementById("detailView");

  detail.classList.remove("hidden");

  document.getElementById("detailTitle").innerText = item.name;
  document.getElementById("detailDesc").innerText = item.description;

  document.getElementById("downloadBtn").href =
    `files/${category.toLowerCase()}/${item.file}`;
}

// BACK
function goBack() {
  document.getElementById("detailView").classList.add("hidden");
  openTab(currentTab);
}

// INIT
renderSlide();
