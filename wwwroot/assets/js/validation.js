function validateForm() {
    // Lấy giá trị các trường
    const fullName = document.getElementById("fullName").value;
    const phone = document.getElementById("phone").value;
    const workTime = document.getElementById("workTime").value;
    const role = document.getElementById("role").value;

    // Validation cho fullName
    if (fullName.length > 50) {
        alert("Họ tên không được vượt quá 50 ký tự.");
        return false;
    }

    // Validation cho phone
    const phoneRegex = /^0[0-9]{9,10}$/;
    if (phone && !phoneRegex.test(phone)) {
        alert("Số điện thoại phải bắt đầu bằng 0 và có 10 hoặc 11 chữ số.");
        return false;
    }

    // Validation cho workTime (chỉ khi role là Mentor)
    if (role === "Mentor" && workTime) {
        const workTimeRegex = /^([0-1][0-9]|2[0-3]):([0-5][0-9])-([0-1][0-9]|2[0-3]):([0-5][0-9])$/;
        if (!workTimeRegex.test(workTime)) {
            alert("Thời gian làm việc phải có định dạng HH:mm-HH:mm (ví dụ: 08:00-17:00).");
            return false;
        }

        // Kiểm tra thời gian bắt đầu và kết thúc
        const [start, end] = workTime.split("-");
        const [startHour, startMinute] = start.split(":").map(Number);
        const [endHour, endMinute] = end.split(":").map(Number);
        const startTime = startHour * 60 + startMinute;
        const endTime = endHour * 60 + endMinute;

        if (endTime <= startTime) {
            alert("Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
            return false;
        }

        // Kiểm tra thời gian rảnh không nằm trong khoảng 23:00-05:00
        if (startHour >= 23 || startHour < 5 || endHour > 23 || endHour < 5) {
            alert("Thời gian làm việc không được nằm trong khoảng từ 23:00 đến 05:00.");
            return false;
        }
    }
    // Validation cho dob
    if (dob) {
        const dobDate = new Date(dob);
        const currentDate = new Date('2025-05-28');

        // Kiểm tra ngày sinh không trong tương lai
        if (dobDate > currentDate) {
            alert("Ngày sinh không được là ngày trong tương lai.");
            return false;
        }

        // Kiểm tra tuổi trên 18 cho Mentor và Parent
        if (role === "Mentor" || role === "Parent") {
            const ageInMilliseconds = currentDate - dobDate;
            const ageInYears = ageInMilliseconds / (1000 * 60 * 60 * 24 * 365.25);
            if (ageInYears < 18) {
                alert("Mentor hoặc Parent phải trên 18 tuổi.");
                return false;
            }
        }
    }


    return true;
}