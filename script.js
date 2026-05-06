const filesData = {
    Scripts: [],
    Packages: [],
    Models: [],
    Materials: []
};

let currentTab = "home";
let currentSlide = 0;

function switchTab(tab) {
    currentTab = tab;
    render();
}

/* ---------------- HOME SLIDESHOW ---------------- */

function getAllFiles() {
    let all = [];
    for (let cat in filesData) {
        filesData[cat].forEach(f => {
            all.push({ ...f, category: cat });
        });
    }
    return all;
}

function renderHome() {
    const files = getAllFiles();

    if (files.length === 0) {
        document.getElementById("content").innerHTML = "<h2 style='text-align:center;'>Nothing Is Here Yet</h2>";
        return;
    }

    const file = files[currentSlide % files.length];

    document.getElementById("content").innerHTML = `
        <div class="slideshow">
            <h2>Featured Downloads</h2>

            <div class="slideshow-box">
                <h3>${file.name}</h3>
                <p>${file.description}</p>
                <a href="files/${file.category}/${file.file}" download>
                    <button>Download</button>
                </a>
            </div>

            <div class="nav-btns">
                <button onclick="prevSlide()">Back</button>
                <button onclick="nextSlide()">Next</button>
            </div>
        </div>
    `;
}

function nextSlide() {
    currentSlide++;
    renderHome();
}

function prevSlide() {
    currentSlide--;
    if (currentSlide < 0) currentSlide = getAllFiles().length - 1;
    renderHome();
}

/* ---------------- CATEGORY VIEW ---------------- */

function renderCategory(cat) {
    const list = filesData[cat];

    if (!list || list.length === 0) {
        document.getElementById("content").innerHTML = "<h2 style='text-align:center;'>Nothing Is Here Yet</h2>";
        return;
    }

    let html = `<div class="list">`;

    list.forEach((item, i) => {
        html += `
            <div class="item">
                <h3>${item.name}</h3>
                <p>${item.description}</p>
                <button onclick="openDetail('${cat}', ${i})">Open</button>
            </div>
        `;
    });

    html += `</div>`;
    document.getElementById("content").innerHTML = html;
}

/* ---------------- DETAIL PAGE ---------------- */

function openDetail(cat, index) {
    const item = filesData[cat][index];

    document.getElementById("content").innerHTML = `
        <div class="detail">
            <h2>${item.name}</h2>
            <p>${item.description}</p>

            <a href="files/${cat}/${item.file}" download>
                <button>Download</button>
            </a>

            <br><br>
            <button onclick="render()">Go Back</button>
        </div>
    `;
}

/* ---------------- MAIN RENDER ---------------- */

function render() {
    if (currentTab === "home") renderHome();
    else renderCategory(currentTab);
}

/* AUTO SLIDESHOW */
setInterval(() => {
    if (currentTab === "home") {
        currentSlide++;
        renderHome();
    }
}, 15000);

/* INIT */
render();
