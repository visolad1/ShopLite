#!/usr/bin/env bash
# End-to-end API smoke test against a running backend + PostgreSQL.
#
# Usage: scripts/smoke-test.sh [API_URL]
#   API_URL defaults to http://localhost:5080
# Requirements: curl, jq, docker (for the PostgreSQL verification step).
set -uo pipefail

API="${1:-http://localhost:5080}"
DB_CONTAINER="${DB_CONTAINER:-ecommerce-db-1}"
DB_USER="${POSTGRES_USER:-ecommerce}"
DB_NAME="${POSTGRES_DB:-ecommerce}"

PASS=0; FAIL=0
EMAIL="smoke_$(date +%s)_$RANDOM@example.com"
PASSWORD="SmokeTest#2026"
TOKEN=""
BODY=""; STATUS=""

ok()   { PASS=$((PASS+1)); printf '  \033[32mPASS\033[0m %s\n' "$1"; }
bad()  { FAIL=$((FAIL+1)); printf '  \033[31mFAIL\033[0m %s\n' "$1"; [ -n "${2:-}" ] && printf '       %s\n' "$2"; }
check(){ if eval "$2"; then ok "$1"; else bad "$1" "status=$STATUS body=$(echo "$BODY" | head -c 300)"; fi; }

# req METHOD PATH [JSON] -> sets STATUS and BODY
req() {
  local method="$1" path="$2" data="${3:-}" auth=()
  [ -n "$TOKEN" ] && auth=(-H "Authorization: Bearer $TOKEN")
  local out
  if [ -n "$data" ]; then
    out=$(curl -s -w '\n%{http_code}' -X "$method" "$API$path" -H 'Content-Type: application/json' ${auth[@]+"${auth[@]}"} -d "$data")
  else
    out=$(curl -s -w '\n%{http_code}' -X "$method" "$API$path" ${auth[@]+"${auth[@]}"})
  fi
  STATUS="${out##*$'\n'}"; BODY="${out%$'\n'*}"
}
j() { echo "$BODY" | jq -r "$1"; }

echo "API: $API   user: $EMAIL"

echo "1. Registration"
req POST /api/auth/register "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\",\"fullName\":\"Smoke Tester\"}"
check "register returns 201 + token" '[ "$STATUS" = 201 ] && [ "$(j .token)" != null ]'
req POST /api/auth/register "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\",\"fullName\":\"Smoke Tester\"}"
check "duplicate email rejected (409)" '[ "$STATUS" = 409 ]'
req POST /api/auth/register '{"email":"not-an-email","password":"123","fullName":""}'
check "invalid registration rejected (400 with field errors)" '[ "$STATUS" = 400 ] && [ "$(j ".errors | length")" -ge 2 ]'

echo "2. Login"
req POST /api/auth/login "{\"email\":\"$EMAIL\",\"password\":\"wrong-password\"}"
check "wrong password rejected (401)" '[ "$STATUS" = 401 ]'
req POST /api/auth/login "{\"email\":\"$(echo "$EMAIL" | tr a-z A-Z)\",\"password\":\"$PASSWORD\"}"
check "login succeeds (email case-insensitive)" '[ "$STATUS" = 200 ] && [ "$(j .token)" != null ]'
TOKEN="$(j .token)"
req GET /api/auth/me
check "profile returns user" '[ "$STATUS" = 200 ] && [ "$(j .email)" = "$EMAIL" ]'

echo "3. Products load"
req GET /api/categories
check "categories load (>= 3)" '[ "$STATUS" = 200 ] && [ "$(j length)" -ge 3 ]'
req GET /api/products
check "products load (>= 10)" '[ "$STATUS" = 200 ] && [ "$(j length)" -ge 10 ]'
req GET "/api/products?featured=true"
check "featured filter" '[ "$STATUS" = 200 ] && [ "$(j "map(select(.isFeatured==false)) | length")" = 0 ] && [ "$(j length)" -ge 1 ]'
req GET "/api/products?categoryId=3"
check "category filter" '[ "$STATUS" = 200 ] && [ "$(j "map(select(.categoryId!=3)) | length")" = 0 ] && [ "$(j length)" -ge 1 ]'

echo "4. Search"
req GET "/api/products?search=HEADPHONES"
check "search is case-insensitive and matches" '[ "$STATUS" = 200 ] && [ "$(j length)" -ge 2 ]'
req GET "/api/products?search=%25zz"
check "search treats % as a literal, not a wildcard" '[ "$STATUS" = 200 ] && [ "$(j length)" = 0 ]'
req GET "/api/products?search=100%25"
check "literal % matches (\"100% organic cotton\")" '[ "$STATUS" = 200 ] && [ "$(j length)" = 1 ]'

echo "5. Product details"
req GET /api/products/1
check "product details load" '[ "$STATUS" = 200 ] && [ "$(j .id)" = 1 ] && [ "$(j .description)" != "" ]'
PRICE1="$(j .price)"; STOCK1_BEFORE="$(j .stock)"
req GET /api/products/99999
check "unknown product returns 404 problem" '[ "$STATUS" = 404 ] && [ "$(j .detail)" = "Product not found." ]'

echo "6. Add to cart"
SAVED_TOKEN="$TOKEN"; TOKEN=""
req GET /api/cart
check "cart requires auth (401)" '[ "$STATUS" = 401 ]'
TOKEN="$SAVED_TOKEN"
req GET /api/cart
check "new user cart is empty" '[ "$STATUS" = 200 ] && [ "$(j ".items | length")" = 0 ] && [ "$(j .total)" = 0 ]'
req POST /api/cart/items '{"productId":1,"quantity":2}'
check "add product 1 x2" '[ "$STATUS" = 200 ] && [ "$(j ".items | length")" = 1 ]'
# Client tries to sneak in a price - must be ignored.
req POST /api/cart/items '{"productId":7,"quantity":1,"price":0.01,"unitPrice":0.01}'
check "add product 7 x1 (client price ignored)" '[ "$STATUS" = 200 ] && [ "$(j ".items | length")" = 2 ]'
req POST /api/cart/items '{"productId":13,"quantity":1}'
check "out-of-stock product rejected (409)" '[ "$STATUS" = 409 ]'
req POST /api/cart/items '{"productId":1,"quantity":1000}'
check "quantity above limit rejected (400)" '[ "$STATUS" = 400 ]'

echo "7. Change quantities"
req GET /api/cart
ITEM1=$(j '.items[] | select(.productId==1) | .id')
ITEM7=$(j '.items[] | select(.productId==7) | .id')
req PATCH "/api/cart/items/$ITEM1" '{"quantity":3}'
check "increase quantity to 3" '[ "$STATUS" = 200 ] && [ "$(j ".items[] | select(.productId==1) | .quantity")" = 3 ]'
req PATCH "/api/cart/items/$ITEM7" '{"quantity":2}'
check "set product 7 quantity to 2" '[ "$STATUS" = 200 ]'
req PATCH "/api/cart/items/$ITEM1" '{"quantity":0}'
check "quantity 0 rejected (400)" '[ "$STATUS" = 400 ]'
req POST /api/cart/items '{"productId":2,"quantity":1}'
ITEM2=$(j '.items[] | select(.productId==2) | .id')
req DELETE "/api/cart/items/$ITEM2"
check "remove item" '[ "$STATUS" = 200 ] && [ "$(j ".items | length")" = 2 ]'
req DELETE "/api/cart/items/999999"
check "remove unknown item (404)" '[ "$STATUS" = 404 ]'

echo "8. Cart total"
req GET /api/cart
EXPECTED=$(jq -n "((($PRICE1 * 3) + (19.99 * 2)) * 100 | round) / 100")
check "total = 3 x $PRICE1 + 2 x 19.99 = $EXPECTED" '[ "$(jq -n "($(j .total) - $EXPECTED) | fabs < 0.001")" = true ] && [ "$(j .totalQuantity)" = 5 ]'
CART_TOTAL="$(j .total)"

echo "9. Submit order"
req POST /api/orders '{"customerName":"","phone":"abc","deliveryAddress":""}'
check "invalid checkout rejected (400)" '[ "$STATUS" = 400 ]'
req POST /api/orders '{"customerName":"Smoke Tester","phone":"+1 555 123 4567","deliveryAddress":"1 Test Street, Springfield","total":1.00}'
check "order placed (201, Pending, CashOnDelivery)" '[ "$STATUS" = 201 ] && [ "$(j .status)" = Pending ] && [ "$(j .paymentMethod)" = CashOnDelivery ]'
ORDER_ID="$(j .id)"
check "order total computed by server ($CART_TOTAL), client total ignored" '[ "$(jq -n "($(j .total) - $CART_TOTAL) | fabs < 0.001")" = true ]'
req GET /api/cart
check "cart emptied after order" '[ "$(j ".items | length")" = 0 ]'
req GET /api/products/1
check "stock decremented ($STOCK1_BEFORE -> $((STOCK1_BEFORE-3)))" '[ "$(j .stock)" = $((STOCK1_BEFORE-3)) ]'
req POST /api/orders '{"customerName":"Smoke Tester","phone":"+1 555 123 4567","deliveryAddress":"1 Test Street"}'
check "ordering with empty cart rejected (400)" '[ "$STATUS" = 400 ]'

echo "10. Order saved in PostgreSQL"
if command -v docker >/dev/null && docker ps --format '{{.Names}}' | grep -qx "$DB_CONTAINER"; then
  ROW=$(docker exec "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -tA -F'|' -c \
    "SELECT o.\"Status\", o.\"Total\", count(i.\"Id\"), sum(i.\"Quantity\") FROM \"Orders\" o JOIN \"OrderItems\" i ON i.\"OrderId\" = o.\"Id\" WHERE o.\"Id\" = $ORDER_ID GROUP BY o.\"Id\";")
  check "DB row: $ROW" '[ "$(echo "$ROW" | cut -d"|" -f1)" = Pending ] && [ "$(echo "$ROW" | cut -d"|" -f3)" = 2 ] && [ "$(echo "$ROW" | cut -d"|" -f4)" = 5 ]'
  SNAP=$(docker exec "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -tA -c \
    "SELECT count(*) FROM \"OrderItems\" WHERE \"OrderId\" = $ORDER_ID AND \"ProductName\" <> '' AND \"UnitPrice\" > 0;")
  check "OrderItems store name + unit price snapshots" '[ "$SNAP" = 2 ]'
else
  bad "PostgreSQL container '$DB_CONTAINER' not running - skipped DB verification"
fi

echo "11. Order history"
req GET /api/orders
check "order history lists the order" '[ "$STATUS" = 200 ] && [ "$(j ".[0].id")" = "$ORDER_ID" ] && [ "$(j ".[0].itemCount")" = 5 ]'
req GET "/api/orders/$ORDER_ID"
check "order details include 2 items" '[ "$STATUS" = 200 ] && [ "$(j ".items | length")" = 2 ]'

echo "    Isolation: another user cannot see this order"
TOKEN=""
req POST /api/auth/register "{\"email\":\"other_$EMAIL\",\"password\":\"$PASSWORD\",\"fullName\":\"Other User\"}"
TOKEN="$(j .token)"
req GET "/api/orders/$ORDER_ID"
check "other user's order returns 404" '[ "$STATUS" = 404 ]'

echo "12. Logout"
# JWTs are stateless: logout = the client discards its token. Requests without it are rejected.
TOKEN=""
req GET /api/orders
check "requests without token are rejected (401)" '[ "$STATUS" = 401 ]'
TOKEN="not.a.valid.token"
req GET /api/orders
check "tampered token rejected (401)" '[ "$STATUS" = 401 ]'

echo
echo "Result: $PASS passed, $FAIL failed"
[ "$FAIL" = 0 ]
