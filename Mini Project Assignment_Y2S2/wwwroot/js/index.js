document.getElementById("postButton").addEventListener("click", () => {

    const bt1 = document.getElementById("foundButton");
    const bt2 = document.getElementById("postButton");

    [bt1.innerText, bt2.innerText] = [bt2.innerText, bt1.innerText];
});

