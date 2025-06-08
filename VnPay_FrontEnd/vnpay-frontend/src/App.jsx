import { useState } from 'react';
import xemay from './assets/xemay.png';
import './App.css';

function App() {
  const [orderInfo, setOrderInfo] = useState("Mua xe máy Honda");
  const [price, setPrice] = useState(102000000);

  const handlePayment = async () => {
    const response = await fetch('https://localhost:7291/api/v0/payment', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ orderInfo, price }),
    });

    if (response.ok) {
      const data = await response.json();
      window.location.href = data.paymentUrl;
    }
  };

  return (
    <div className="app-container">
      <div className="content">
        <img src={xemay} alt="Motorcycle" className="motorcycle-image" />
        <h2 className="motorcycle-name">SH 160i</h2>
        <p className="description">
          Kế thừa tinh hoa của dòng xe SH với những đường nét thanh lịch, sang trọng mang hơi thở Châu Âu cùng động cơ cải tiến đột phá và công nghệ tiên tiến, SH160i/125i mới sở hữu diện mạo đầy ấn tượng và nổi bật.
        </p>
        <p className="price"><strong>Price:</strong> {price.toLocaleString()} VND</p>
        <p className="color"><strong>Color:</strong> Gray</p>
        <button onClick={handlePayment} className="payment-button">
          Proceed to Payment
        </button>
      </div>
    </div>
  );
}

export default App;