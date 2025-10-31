import pandas as pd

# This program processes churn data from a CSV file, sorts it by total churn,
# and filters for entries related to the 'homeassistant/components/' folder.

data = pd.read_csv('churn_per_folder.csv', encoding='utf-16') #Import churn data
sorted = data.sort_values(by='TotalChurn', ascending=False) #Sort by Total Churn
print(sorted.head()) #Print top 5 rows

onlyComps = data[data['Folder'].str.match("homeassistant/components/")] #Filter for commits made in the components folder
print(onlyComps.head()) #Print top 5 rows of filtered data