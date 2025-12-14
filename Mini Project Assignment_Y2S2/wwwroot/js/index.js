
const back = document.getElementById("pagingBack");
const next = document.getElementById("pagingNext");

back.disabled = true;

function calculatePageSize() {
    const width = window.innerWidth;
    const height = window.innerHeight;
    let size;

    if (height > 800) {
        if (width > 1883) size = 10;
        else if (width > 1595) size = 8;
        else if (width > 1304) size = 6;
        else if (width > 1014) size = 4;
        else size = 2;
    } else {
        if (width > 1883) size = 5;
        else if (width > 1595) size = 4;
        else if (width > 1260) size = 3;
        else if (width > 1014) size = 2;
        else size = 1;
    }

    return Math.max(1, Math.min(10, size));
}

async function checkTotal() {
    const response = await fetch(`/Home/totalItem`);
    const data = await response.json();
    // 不在这里除以固定值，因为每页size是动态的
    return data.totalCount; // 只返回总数量
}

let currentPaging = localStorage.getItem("currentPaging")
    ? parseInt(localStorage.getItem("currentPaging"))
    : 1;
refresh()
function refresh(){
    const b1 = document.getElementById("postButton");
    const b2 = document.getElementById("foundButton");

    if (b1.innerHTML != "Found") {
        [b1.innerHTML, b2.innerHTML] = [b2.innerHTML, b1.innerHTML];
        [b1.value, b2.value] = [b2.value, b1.value];
    }

    document.getElementById("currentPage").innerHTML = 1;
    back.disabled = true;
    next.disabled = false;

    currentPaging = 1
    localStorage.setItem("currentPaging", 1);
    loadPage(currentPaging, null, null, null);
}

document.getElementById("index").addEventListener("click", () => {
    refresh()
})

document.getElementById("postButton").addEventListener("click", () => {
    

    const b1 = document.getElementById("postButton");
    const b2 = document.getElementById("foundButton");

    document.getElementById("currentPage").innerHTML = 1;
    back.disabled = true;
    next.disabled = false;

    [b1.innerHTML, b2.innerHTML] = [b2.innerHTML, b1.innerHTML];
    [b1.value, b2.value] = [b2.value, b1.value];
    currentPaging = 1
    localStorage.setItem("currentPaging", 1);
    filterCard(b2.value,null,null,null);
});

const temp = document.getElementById("tempdata");
if (temp) {
    setTimeout(() => {
        temp.classList.add("hide");
        setTimeout(() => {
            temp.style.display = "none";
        }, 1000);
    }, 3000);
}



document.getElementById("filter").addEventListener("click", () => {

    const b1 = document.getElementById("foundButton");


    const startDate = document.getElementById("startDate").value;
    const endDate = document.getElementById("endDate").value;
    const locationID = document.getElementById("location").value;

    let category;
    if (b1.value == "FOUNDITEM") {
        category = "FOUNDITEM";
    } else {
        category = "LOSTITEM";
    }
    currentPaging = 1
    localStorage.setItem("currentPaging", 1);
    filterCard(category, startDate, endDate,locationID);
    
})

async function filterCard(category, startDate, endDate, locationID) {
    // 1️⃣ fetch filter 结果
    const response = await fetch(`/Home/filter?category=${category}&startDate=${startDate}&endDate=${endDate}&locationID=${locationID}`);
    const html = await response.text();
    document.querySelector(".cardparent").innerHTML = html;

    // 2️⃣ 页码重置
    currentPaging = 1;
    localStorage.setItem("currentPaging", 1);
    document.getElementById("currentPage").innerHTML = 1;

    // 3️⃣ 获取总数量（不是总页数）
    const totalCount = await checkTotal();  // ✅ 返回的是总数量

    // 4️⃣ 需要计算当前size来计算总页数


    const size = calculatePageSize(); // 替换原来的逻辑

    // 5️⃣ 计算总页数并设置按钮状态
    const totalPages = Math.ceil(totalCount / size);
    back.disabled = true;
    next.disabled = totalPages <= 1;
}

const startDate = document.getElementById("startDate");
const endDate = document.getElementById("endDate");


startDate.addEventListener("change", () => {
    endDate.min = startDate.value;


    if (endDate.value < startDate.value) {
        endDate.value = startDate.value;
    }
});


endDate.addEventListener("change", () => {
    if (endDate.value < startDate.value) {
        endDate.value = startDate.value;
    }
});


//paging


document.getElementById("pagingNext").addEventListener("click", async () => {
    const size = calculatePageSize();

    const totalCount = await checkTotal();
    const totalPages = Math.ceil(totalCount / size);

    if (currentPaging < totalPages) {
        currentPaging++;
        loadPage(currentPaging);
    } else {
        console.log("已是最后一页");
        return;
    }
});

document.getElementById("pagingBack").addEventListener("click", async () => {
    if (currentPaging > 1) {
        currentPaging--;
        loadPage(currentPaging);
    } else {
        console.log("已是第一页");
        return;
    }
});

let lastDevicePixelRatio = window.devicePixelRatio;
let lastInnerWidth = window.innerWidth;

function checkZoomChange() {
    const currentDPR = window.devicePixelRatio;
    const currentWidth = window.innerWidth;

    // 如果devicePixelRatio变化或innerWidth变化较大，可能是缩放
    if (currentDPR !== lastDevicePixelRatio ||
        Math.abs(currentWidth - lastInnerWidth) > 50) {

        console.log(`🔄 检测到缩放: DPR ${lastDevicePixelRatio} → ${currentDPR}`);
        lastDevicePixelRatio = currentDPR;
        lastInnerWidth = currentWidth;

        // 重新加载当前页
        if (currentPaging) {
            loadPage(currentPaging);
        }
    }
}

// 监听resize（缩放会触发resize）
let resizeTimer;
window.addEventListener('resize', () => {
    clearTimeout(resizeTimer);
    resizeTimer = setTimeout(() => {
        checkZoomChange();
    }, 200);
});

// 在fetch后添加错误处理
async function loadPage(page) {
    try {
        const size = calculatePageSize();
        console.log(`🔍 加载第 ${page} 页，每页 ${size} 个`);

        const totalCount = await checkTotal();
        console.log(`📊 总数据量: ${totalCount}`);

        const response = await fetch(`/Home/IndexPaging?page=${page}&size=${size}`);

        if (!response.ok) {
            console.error(`❌ 请求失败: ${response.status} ${response.statusText}`);
            return;
        }

        const html = await response.text();
        console.log(`✅ 获取到HTML长度: ${html.length} 字符`);

        if (!html || html.trim().length === 0) {
            console.warn(`⚠️ 返回的HTML为空或过短`);
        }

        document.querySelector(".cardparent").innerHTML = html;
        document.getElementById("currentPage").innerHTML = page;

        currentPaging = page;
        localStorage.setItem("currentPaging", page);

        const totalPages = Math.ceil(totalCount / size);
        console.log(`📄 总页数: ${totalPages}`);

        document.getElementById("pagingBack").disabled = page <= 1;
        document.getElementById("pagingNext").disabled = page >= totalPages;

    } catch (error) {
        console.error(`🚨 loadPage错误:`, error);
    }
}
document.getElementById("resetFilter").addEventListener("click", () => {
    // 1️⃣ 重置 Location
    document.getElementById("location").value = "";

    // 2️⃣ 重置日期
    document.getElementById("startDate").value = "";
    document.getElementById("endDate").value = "";

    // 3️⃣ 页码回到 1
    currentPaging = 1;
    localStorage.setItem("currentPaging", 1);

    // 4️⃣ 直接加载第一页（等于“清空筛选”）
    loadPage(1);
});